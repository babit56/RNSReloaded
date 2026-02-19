using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using Reloaded.Imgui.Hook;
using Reloaded.Imgui.Hook.Direct3D11;
using DearImguiSharp;
namespace RNSReloaded.DamageTracker;

public unsafe class Mod : IMod {
    private WeakReference<IRNSReloaded>? rnsReloadedRef;
    private WeakReference<IReloadedHooks>? hooksRef;
    private ILoggerV1 logger = null!;

    private IHook<ScriptDelegate>? damageHook;
    private IHook<ScriptDelegate>? newFightHook;

    private struct DamageInfo {
        public long damage;
        public long count;
    }

    private Dictionary<long, DamageInfo>[] damageAmounts = [
        new Dictionary<long, DamageInfo>(),
        new Dictionary<long, DamageInfo>(),
        new Dictionary<long, DamageInfo>(),
        new Dictionary<long, DamageInfo>()
    ];

    private Dictionary<long, string> hbIdNameOverrides = new Dictionary<long, string>() {
        { -1, "Debuffs" },
        { 1, "Primary" },
        { 2, "Secondary" },
        { 3, "Special" },
        { 4, "Defensive" }
    };

    public void StartEx(IModLoaderV1 loader,IModConfigV1 modConfig) {
        this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
        this.hooksRef = loader.GetController<IReloadedHooks>()!;
        
        this.logger = loader.GetLogger();

        if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            rnsReloaded.OnReady += this.Ready;
        }

        if (this.hooksRef != null && this.hooksRef.TryGetTarget(out var hooks)) {
            SDK.Init(hooks);
            ImguiHook.Create(this.Draw, new ImguiHookOptions() {
                Implementations = [new ImguiHookDx11()]
            });
        }

        this.logger.PrintMessage("Set up discount ACT", this.logger.ColorGreen);
    }

    private bool ready = false;
    public void Ready() {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
        ) {
            this.ready = true;

            var damageScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_pattern_deal_damage_enemy_subtract") - 100000);
            this.damageHook =
                hooks.CreateHook<ScriptDelegate>(this.EnemyDamageDetour, damageScript->Functions->Function);
            this.damageHook.Activate();
            this.damageHook.Enable();

            var newFightScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scrdt_encounter") - 100000);
            this.newFightHook =
                hooks.CreateHook<ScriptDelegate>(this.NewFightDetour, newFightScript->Functions->Function);
            this.newFightHook.Activate();
            this.newFightHook.Enable();
        }
    }

    private double? initialPainshare = null;
    private RValue* EnemyDamageDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            var hbId = rnsReloaded.utils.RValueToLong(rnsReloaded.FindValue(self, "hbId"));
            var damage = rnsReloaded.utils.RValueToLong(argv[2]);
            var playerId = rnsReloaded.utils.RValueToLong(rnsReloaded.FindValue(self, "playerId"));
            var enemyId = rnsReloaded.utils.RValueToLong(argv[1]);

            // Find painshare ratio on first enemy and cache it for if it changes to 0 later
            // (this happens to mell p2 only, currently, but it means we don't care about future summons)
            if (!this.initialPainshare.HasValue) {
                var painShare = rnsReloaded.utils.RValueToDouble(rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "playerPainshareRatio")->Get(1)->Get(0));
                this.initialPainshare = painShare;
            }
            if (enemyId == 0 || this.initialPainshare == 0) {
                var dmgInfo = this.damageAmounts[playerId].GetValueOrDefault(hbId, new DamageInfo() { count = 0, damage = 0 });
                dmgInfo.count++;
                dmgInfo.damage += damage;
                this.damageAmounts[playerId][hbId] = dmgInfo;
            }
        }

        returnValue = this.damageHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* NewFightDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        this.initialPainshare = null;
        foreach (var dict in this.damageAmounts) {
            dict.Clear();
        }
        returnValue = this.newFightHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private int selectedPlayer = 0;
    public void Draw() {
        if (!this.ready) return;
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            var open = true;

            var buttonSize = new ImVec2 {
                X = 0,
                Y = 0
            };
            if (ImGui.Begin("Damage", ref open, 0)) {
                for (int i = 0; i < 4; i++) {
                    ImGui.SameLine(0, i == 0 ? 0 : 10);
                    string playerName = rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "playerName")->Get(0)->Get(i)->ToString();
                    if (this.selectedPlayer == i) {
                        ImGui.Text(playerName);
                    } else {
                        if (ImGui.Button(playerName, buttonSize)) {
                            this.selectedPlayer = i;
                        }
                    }
                }

                if (ImGui.BeginTable("", 4, 384, buttonSize, 0)) {
                    var orderedKeys = this.damageAmounts[this.selectedPlayer].Keys.Order();
                    ImGui.TableNextRow(0, 0);
                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Source");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Damage");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("Hits");

                    ImGui.TableNextColumn();
                    ImGui.TableHeader("% Total");
                    long totalDamage = this.damageAmounts[this.selectedPlayer].Values.Sum(x => x.damage);
                    foreach (var key in orderedKeys) {
                        // getting Potion #5 for 2nd player primary (debuffs still debuffs)
                        var adjustedKey = key;
                        if (adjustedKey > 0) {
                            adjustedKey -= this.selectedPlayer * 14;
                        }
                        string hbName = this.hbIdNameOverrides.GetValueOrDefault(adjustedKey, "");

                        /* This is wrong, it's going from hbId -> itemIdString, needs to go hbId -> itemId -> itemName

                        if (hbName == "") {
                            var dataMap = rnsReloaded.utils.GetGlobalVar("itemData");
                            hbName = dataMap->Get((int) key)->Get(0)->Get(0)->ToString();
                        }
                        */

                        if (hbName == "") {
                            if (key > 4 && key < 11) {
                                hbName = "Item #" + (adjustedKey - 4);
                            } else if (key >= 11) {
                                hbName = "Potion #" + (adjustedKey - 10);
                            } else {
                                hbName = "HB id " + adjustedKey;
                            }
                        }
                        long damage = this.damageAmounts[this.selectedPlayer][key].damage;
                        long hits = this.damageAmounts[this.selectedPlayer][key].count;
                        ImGui.TableNextRow(0, 0);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{hbName}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{damage}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{hits}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{Math.Round(100f * damage / totalDamage, 1)}%");
                    }
                    ImGui.EndTable();
                }
            }
            ImGui.End();
        }
    }

    public void Suspend() {
        this.damageHook?.Disable();
    }

    public void Resume() {
        this.damageHook?.Enable();
    }

    public bool CanSuspend() => false; // Add suspend/resume code and set to true once ready

    public void Unload() { }
    public bool CanUnload() => false;

    public Action Disposing => () => { };
}
