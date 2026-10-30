using Reloaded.Hooks.Definitions;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.HostDT.Config;
using System.Runtime.InteropServices;
using System.Text;
// using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using System.Drawing;

namespace RNSReloaded.HostDT;

// public unsafe delegate void ArrayErrorDelegate(char* inp, char a, char b);

public unsafe class Mod : IMod {
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private IRNSReloaded rnsReloaded;
    private IReloadedHooks hooks;
    // private IStartupScanner scannerRef;
    #pragma warning restore CS8618
    private ILoggerV1 logger = null!;

    private Dictionary<string, RValue> bpatt_var_local = new Dictionary<string, RValue>();
    private IHook<ScriptDelegate>? bpattVarHook;
    private IHook<ScriptDelegate>? bpattVarResetHook;
    private IHook<ScriptDelegate>? bpattAddHook;

    private IHook<ScriptDelegate>? chatHook;
    private IHook<ScriptDelegate>? zoomHook;
    private IHook<ScriptDelegate>? gameOverHoook;
    private IHook<ScriptDelegate>? gameRestartHook;
    private IHook<ScriptDelegate>? addPatternHook;
    private IHook<ScriptDelegate>? bpswMultiHook;
    private IHook<ScriptDelegate>? gameframeHook;
    private IHook<ScriptDelegate>? randomHook;
    private IHook<ScriptDelegate>? merranSlowHook;
    private IHook<ScriptDelegate>? enemyHpHook;
    private IHook<ScriptDelegate>? battleStartHook;
    private IHook<ScriptDelegate>? nothingHook;
    private IHook<ScriptDelegate>? gamespeedHook;
    private IHook<ScriptDelegate>? gamespeedTempHook;
    private IHook<ScriptDelegate>? extraHook;
    private IHook<ScriptDelegate>? resetEncsHook;
    private IHook<ScriptDelegate>? defEncHook;
    private IHook<ScriptDelegate>? spEncHook;
    private IHook<ScriptDelegate>? mpEncHook;
    private IHook<ScriptDelegate>? extraEncHook;
    private IHook<ScriptDelegate>? langStringHook;
    private IHook<RoutineDelegate>? audioHook;

    private static Dictionary<string, IHook<ScriptDelegate>> ScriptHooks = new();

    private Configurator configurator = null!;
    private Config.Config config = null!;
    private static readonly double DefaultSpeed = 1;
    private static readonly double DefaultZoom = 1;
    private static readonly double DefaultWinds = 0;
    private static readonly double DefaultGrav = 0;
    private static readonly int DefaultHeavy = 0;
    private static readonly int DefaultBulletHell = 0;
    private static readonly int DefaultChaos = 0;
    private static readonly double DefaultSoloTogether = 0;
    private static readonly double DefaultHpMult = 1;
    private double speed = DefaultSpeed;
    private double zoom = DefaultZoom;
    private double winds = DefaultWinds;
    private double grav = DefaultGrav;
    private int heavy = DefaultHeavy;
    private int bulletHell = DefaultBulletHell;
    private int chaos = DefaultChaos;
    private double soloTogether = DefaultSoloTogether;
    private double hpMult = DefaultHpMult;
    private bool isStartingCombat = false;
    private bool delayedSpeedchange = false;
    private bool clientJoined = false;
    private RValue* clientOwnId;
    private bool isHost => this.clientOwnId != null && (int)*this.clientOwnId == 0;
    private bool hostHooksEnabled = true;

    private double extraMechMode = 1.0;
    private bool imAddingMech = false;
    private bool imResettingVar = false;
    private bool imSettingVar = false;

    private string afkMessage = "";
    private List<List<RValue>> spPatterns = [];
    private List<List<RValue>> mpPatterns = [];
    private Dictionary<(RValue, int), RValue> mpToSpMap = [];

    private Random random = new();
    private int counter = 300;
    private int counterAfk = 1000;
    private bool restarting = false;

    //chaos stuff
    private double chaosZoom = 1.0;
    private int nextTime = 0;
    private int nextMech = 0;
    private int encounter = 0;
    private int mech = 0;
    private int[][]? ints;
    private int getRandInt() => this.random.Next(0, maxValue: int.MaxValue);

    private RValue? audioStream;

    public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig) {
        var rnsReloadedRef = loader.GetController<IRNSReloaded>();
        var hooksRef = loader.GetController<IReloadedHooks>();
        // this.scannerRef = loader.GetController<IStartupScanner>();
        if (rnsReloadedRef == null || !rnsReloadedRef.TryGetTarget(out this.rnsReloaded!)) {
            throw new Exception("RNSReloaded not available, not loading mod");
        }
        if (hooksRef == null || !hooksRef.TryGetTarget(out this.hooks!)) {
            throw new Exception("RNSReloaded Hooks not available, not loading mod");
        }
        this.logger = loader.GetLogger();

        this.rnsReloaded.OnReady += this.Ready;

        this.configurator = new Configurator(((IModLoader) loader).GetModConfigDirectory(modConfig.ModId));
        this.config = this.configurator.GetConfiguration<Config.Config>(0);
        this.config.ConfigurationUpdated += this.ConfigurationUpdated;
    }

    private void ConfigurationUpdated(IUpdatableConfigurable newConfig) {
        this.config = (Config.Config) newConfig;
    }

    public void Ready() {
        Console.OutputEncoding = Encoding.Unicode;

        this.rnsReloaded.registerVanillaMod("HostDT");

        this.rnsReloaded.addScriptHook("scr_battlecontroller_start_round", this.BattleStartDetour, out this.battleStartHook);
        this.rnsReloaded.addScriptHook("scr_enemy_add_pattern", this.AddPatternDetour, out this.addPatternHook);
        this.rnsReloaded.addScriptHook("bpsw_multiplayer", this.BpswMultiDetour, out this.bpswMultiHook);
        this.rnsReloaded.addScriptHook("bp_nothing", this.BpNothingDetour, out this.nothingHook);
        this.rnsReloaded.addScriptHook("bp_wolf_steeltooth_faceoff", this.MerranSlowDetour, out this.merranSlowHook);
        this.rnsReloaded.addScriptHook("scrbp_zoom", this.ZoomDetour, out this.zoomHook);
        this.rnsReloaded.addScriptHook("bp_setgamespeed", this.GamespeedDetour, out this.gamespeedHook);
        this.rnsReloaded.addScriptHook("bp_setgamespeed_temp", this.GamespeedTempDetour, out this.gamespeedTempHook);
        this.rnsReloaded.addScriptHook("scr_enemy_set_pattern_health", this.enemyHpDetour, out this.enemyHpHook);
        this.rnsReloaded.addScriptHook("scr_chat_add_message", this.ChatDetour, out this.chatHook);
        this.rnsReloaded.addScriptHook("gameframe_update", this.GameframeDetour, out this.gameframeHook);
        this.rnsReloaded.addScriptHook("scr_runmenu_victory_returnhost", this.GameRestartDetour, out this.gameRestartHook);
        this.rnsReloaded.addScriptHook("scr_runmenu_victorydefeat_returnbuttons", this.GameOverDetour, out this.gameOverHoook);
        this.rnsReloaded.addScriptHook("scr_hallwayprogress_choose_halls", this.MakeRandomDetour, out this.randomHook);
        this.rnsReloaded.addScriptHook("scr_encounter_internal_data", this.resetEncsDetour, out this.resetEncsHook);
        this.rnsReloaded.addScriptHook("screnc_data_set", this.defEncDetour, out this.defEncHook);
        this.rnsReloaded.addScriptHook("screnc_data_pattern_sp", this.spEncDetour, out this.spEncHook);
        this.rnsReloaded.addScriptHook("screnc_data_pattern_mp", this.mpEncDetour, out this.mpEncHook);
        this.rnsReloaded.addScriptHook("screnc_data_pattern", this.extraEncDetour, out this.extraEncHook);
        this.rnsReloaded.addScriptHook("scr_lang_string", this.langStringDetour, out this.langStringHook);
        // this.rnsReloaded.addScriptHook("scr_encounter_internal_data", this.ExtraDetour, out this.extraHook);
        // this.rnsReloaded.addScriptHook("bpatt_var", this.BpattVarDetour, out this.bpattVarHook);
        // this.rnsReloaded.addScriptHook("bpatt_var_reset", this.BpattVarResetDetour, out this.bpattVarResetHook);
        // this.rnsReloaded.addScriptHook("bpatt_add", this.BpattAddDetour, out this.bpattAddHook);

        // this.rnsReloaded.addRoutineHook("audio_play_sound_at", this.AudioDetour, out this.audioHook);
        // this.audioStream = this.rnsReloaded.ExecuteCodeFunction("audio_create_stream", ["gunshot.ogg"]);
    }

    // public void Scan(string sig, Action<nint> callback) {
    //     if (this.scannerRef!.TryGetTarget(out var scanner)) {
    //         scanner.AddMainModuleScan(sig, status => {
    //             if (status.Found) {
    //                 var BaseAddr = Process.GetCurrentProcess().MainModule!.BaseAddress;
    //                 var addr = BaseAddr + status.Offset;
    //                 if (sig.StartsWith("E8") || sig.StartsWith("E9")) {
    //                     addr += 5 + Marshal.ReadInt32(addr + 1);
    //                 }
    //                 callback(addr);
    //             } else {
    //                 this.logger.PrintMessage($"Failed to scan {sig}", Color.Red);
    //             }
    //         });
    //     }
    // }

    // public void ArrayErrorDetour(char* str, char a, char b) {
    //     this.arrayErrorHook!.OriginalFunction(str, a, b);
    // }

    private RValue* ExtraDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        // this.LogDebug("Extra hook", Color.Gray);
        // if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
        //     var instance = (RValue)self;
        //     var vars = instance["vars"];
        //     this.LogDebug(vars->ToString());
        //     this.LogDebug(rnsReloaded.ExecuteCodeFunction("ds_map_keys_to_array", self, other, [*vars]).ToString()!);
        //     this.LogDebug(rnsReloaded.ExecuteCodeFunction("ds_map_values_to_array", self, other, [*vars]).ToString()!);
        //     this.LogDebug(rnsReloaded.ExecuteCodeFunction("ds_map_find_value", self, other, [*vars, new RValue("layer")]).ToString()!);
        //     this.LogDebug(instance.ToString());

        //     foreach (var layer in rnsReloaded.GetCurrentRoom()->Layers) {
        //         var name = layer->Name == null ? "Unnamed" : Marshal.PtrToStringAnsi((nint) layer->Name)!;
        //         foreach(var inst in layer->GetInstances()) {
        //             foreach (var key in rnsReloaded.GetStructKeys(&inst)) {
        //                 if (key.Contains("bulletPattern")) {
        //                     this.LogDebug($"Layer {layer->ID} ({name})");
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        // }
        // var instR = (RValue)self;
        // this.LogDebug($"actionScript: {*instR["actionScript"]}. Ended={*instR["patternEnded"]}. interval: {*instR["intervalStart"]}-{*instR["intervalEnd"]} <-> patternNextUpdate: {*instR["patternNextUpdate"]}. NextCheck={instR["patternNextCheck"]->ToString()}, Team/Player IDs={*instR["teamId"]}/{*instR["playerId"]}", Color.Gray);
        returnValue = this.extraHook!.OriginalFunction(self, other, returnValue, argc, argv);
        // this.LogDebug($"After. Ended={*instR["patternEnded"]}. interval: {*instR["intervalStart"]}-{*instR["intervalEnd"]} <-> patternNextUpdate: {*instR["patternNextUpdate"]}. NextCheck={instR["patternNextCheck"]->ToString()}, Team/Player IDs={*instR["teamId"]}/{*instR["playerId"]}", Color.Gray);
        // var val = (RValue)rnsReloaded.ScriptFindId("bp_nothing");
        // var arg = &val;
        // this.addPatternHook!.Disable();
        // this.addPatternHook!.OriginalFunction(self, other, returnValue, 1, &arg);
        // this.addPatternHook.Enable();
        // rnsReloaded.ExecuteScript("bp_nothing", self, other, []);
        // var listId = 475;
        try {
        //     var length = this.rnsReloaded.ExecuteCodeFunction("ds_list_size", null, null, [listId]) ?? 0;
        //     Console.Write("[ ");
        //     for (var i = 0; i < (int)length; i++) {
        //         var val = this.rnsReloaded.ExecuteCodeFunction("ds_list_find_value", null, null, [listId, i]) ?? new RValue();
        //         Console.Write($"{val}, ");
        //     }
        //     Console.WriteLine("]");'
            // this.LogDebug($"{self->ID}, {self->layerID}, {self->objectIndex}");
            // this.LogDebug(this.rnsReloaded.ExecuteCodeFunction("object_get_name", [self->objectIndex]).ToString() ?? "youch");
            // this.LogDebug(this.rnsReloaded.ExecuteCodeFunction("object_get_parent", [self->objectIndex]).ToString() ?? "youch");
            var scripts = this.rnsReloaded.ExecuteCodeFunction("asset_get_ids", [5])!.Value;
            foreach (var script in scripts.IterateArray()) {
                var name = this.rnsReloaded.ExecuteCodeFunction("script_get_name", [script])!.Value.ToString();
                File.AppendAllLines("scripts.txt", [name]);
                // this.LogDebug(script.ToString());
            }

        } catch (Exception e) {
            this.logger.PrintMessage(e.ToString(), Color.Red);
        }

        return returnValue;
    }

    private void AudioDetour(RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv) {
        var args = new RValue[argc];
        args[0] = this.audioStream!.Value;
        for (var i = 1; i < argc; i++) {
            args[i] = argv[i];
        }
        fixed (RValue* ptr = args) {
            this.audioHook!.OriginalFunction(returnValue, self, other, argc, ptr);
        }
    }

    #region Rewrite
    private RValue* BpNothingDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        this.LogDebug("Entering bp_nothing", Color.Gray);
        var instR = (RValue)self;
        // Don't erase this pattern on e.g. boss phase change
        RValue trueR = true;
        instR["noPhaseErase"] = &trueR;
        if (this.rnsReloaded.battleScripts.time(self, other, 0)) {
            if (this.zoom != 1.0) {
                this.LogDebug($"DEBUG: Changing zoom to {this.zoom}");
                this.zoomHook!.Disable();
                this.rnsReloaded.battlePatterns.setzoom(self, other, this.zoom);
                this.zoomHook!.Enable();
            }

            if (this.speed != 1.0) {
                this.LogDebug($"DEBUG: Changing speed to {this.speed}");
                this.gamespeedHook!.Disable();
                this.rnsReloaded.battlePatterns.setgamespeed(self, other, timeMult: this.speed);
                this.gamespeedHook!.Enable();
            }
        }
        if (this.chaos == 0 && this.rnsReloaded.battleScripts.time_repeating(self, other, 0, 1000)) {
            // Get the id for the permanent version of the statuses
            var hbsNameIdMap = *this.rnsReloaded.FindGlobalValue("hbsDataKeyMap");
            RValue windsId = this.rnsReloaded.ExecuteCodeFunction("ds_map_find_value", [hbsNameIdMap, "hbs_tailwind2"]) ?? 0;
            var gravId = this.rnsReloaded.ExecuteCodeFunction("ds_map_find_value", [hbsNameIdMap, "hbs_gravity"]) ?? 0;
            var heavyId = this.rnsReloaded.ExecuteCodeFunction("ds_map_find_value", [hbsNameIdMap, "hbs_heavy"]) ?? 0;
            var extraHeavyId = this.rnsReloaded.ExecuteCodeFunction("ds_map_find_value", [hbsNameIdMap, "hbs_heavyextra"]) ?? 0;

            var hasWinds = false;
            var hasGrav = false;
            var hasHeavy = false;
            foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
                foreach (var instance in layer->GetInstances()) {
                    var statusId = instance["statusId"];
                    var aflPlayerId = instance["aflPlayerId"];
                    var aflTeamId = instance["aflTeamId"];
                    // Probably not a hbstatus instance
                    if (statusId == null || aflPlayerId == null || aflTeamId == null) continue;
                    // Host should be the one afflicted
                    if ((int)*aflTeamId != 0 || (int)*aflPlayerId != 0) continue;

                    var intId = (int)*statusId;
                    if (intId == (int)windsId) hasWinds = true;
                    if (intId == (int)gravId) hasGrav = true;
                    if (intId == (int)heavyId || intId == (int)extraHeavyId) hasHeavy = true;
                }
            }
            if (hasWinds) this.LogDebug("Found existing wind debuff");
            if (hasGrav) this.LogDebug("Found existing gravity debuff");
            if (hasHeavy) this.LogDebug("Found existing heavy debuff");
            if (this.winds > 0 && !hasWinds) {
                this.LogDebug($"DEBUG: Changing winds to {this.winds}");
                this.rnsReloaded.battlePatterns.tailwind_permanent(self, other, mult: this.winds);
            }

            if (this.grav > 0 && !hasGrav) {
                this.LogDebug($"DEBUG: Changing gravity pull to {this.grav} towards screen center");
                this.rnsReloaded.battlePatterns.gravity_pull(self, other, mult: this.grav, position: (1920.0 / 2, 1080.0 / 2));
            }

            if (this.heavy > 0 && !hasHeavy) {
                if (this.heavy == 1) {
                    this.LogDebug($"DEBUG: Turning on heavy");
                    this.rnsReloaded.battlePatterns.heavy(self, other, targetMask: 127);
                } else {
                    this.LogDebug($"DEBUG: Turning on very heavy");
                    this.rnsReloaded.battlePatterns.heavyextra(self, other, targetMask: 127);
                }
            }

        }

        # region random bullet mode
        if (this.bulletHell > 0) {
            this.LogDebug("DEBUG: Doing bh stuff");
            if (this.rnsReloaded.battleScripts.time_repeating(self, other, 3000, 6000 - this.bulletHell)) {
                this.LogDebug("DEBUG: Spawning bullet line");
                var rnd = new Random();
                var time = rnd.Next(500);
                var spd = 9 - (int) Math.Sqrt(rnd.Next(4, 65));
                var angle = rnd.Next(-65, 65);
                var angle2 = rnd.Next(-45, 45);
                var numBullets = rnd.Next(7, 25);
                var type = rnd.Next(0, 2);
                if (spd > 5) {
                    type = 2;
                }
                switch (rnd.Next(2)) {
                    case 0:
                        this.rnsReloaded.battlePatterns.light_line(self, other, warningDelay: time, spawnDelay: time, position: (2040, -1000), lineAngle: 90, angle: 180 - angle2, lineLength: 3000, numBullets: numBullets, type: type, showWarning: true, spd: spd);
                        this.rnsReloaded.battlePatterns.light_line(self, other, warningDelay: time, spawnDelay: time, position: (-120, -1000), lineAngle: 90, angle: angle2, lineLength: 3000, numBullets: numBullets, type: type, showWarning: true, spd: spd);

                        break;
                    case 1:
                        this.rnsReloaded.battlePatterns.light_line(self, other, warningDelay: time, spawnDelay: time, position: (-2000, -120), lineAngle: 0, angle: 90 + angle, lineLength: 6000, numBullets: numBullets, type: type, showWarning: true, spd: spd);
                        this.rnsReloaded.battlePatterns.light_line(self, other, warningDelay: time, spawnDelay: time, position: (-2000, 1200), lineAngle: 0, angle: angle - 90, lineLength: 6000, numBullets: numBullets, type: type, showWarning: true, spd: spd);

                        break;
                }
            }
        }
        #endregion

        if (this.chaos != 0) {
            this.ChaosMode(self, other, this.rnsReloaded);
        }

        returnValue = this.nothingHook!.OriginalFunction(self, other, returnValue, argc, argv);
        instR["patternEnded"]->Real = 0.0; // Don't end bp_nothing
        this.isStartingCombat = false;
        return returnValue;
    }

    private RValue* AddPatternDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        if (this.soloTogether != 0) {
            // Surely there's a better way to get difficulty
            var saveInfo = *this.rnsReloaded.FindGlobalValue("saveInfo");
            var difficulty = (int)*saveInfo[8];
            var pattern = *argv[0];
            if (!this.mpToSpMap.ContainsKey((pattern, difficulty))) {
                if (!this.mpToSpMap.ContainsValue(pattern)) this.LogDebug($"Pattern is not in mp to sp pattern map: {pattern}");
                returnValue = this.addPatternHook!.OriginalFunction(self, other, returnValue, argc, argv);
            } else if (this.soloTogether < 1) {
                // Randomly do solotogether instead of multiplayer, with soloTogether as chance
                // Seed based on game seed, area and encounter/notch in area
                var currentPos = 0;
                var hallwayPos = 0;
                foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
                    if (layer->GetName() != "RunMenu_Blocker") { continue; }
                    foreach (var instance in layer->GetInstances()) {
                        foreach (var key in this.rnsReloaded.GetStructKeys(&instance)) {
                            var val = this.rnsReloaded.FindValue(instance.Object, key);
                            if (key == "currentPos") {
                                currentPos = val->ToInt();
                            }
                            if (key == "hallwayPos") {
                                hallwayPos = val->ToInt();
                            }
                        }
                    }
                }
                var mapSeed = this.rnsReloaded.FindGlobalValue("mapSeed")->ToInt();
                var rand = new Random(HashCode.Combine(mapSeed, hallwayPos, currentPos));
                var roll = rand.NextDouble();
                this.LogDebug($"DEBUG: rng st rolled, got comparison: {roll} < {this.soloTogether}");
                if (roll < this.soloTogether) {
                    var sp_pattern = this.mpToSpMap[(pattern, difficulty)];
                    this.LogDebug($"DEBUG: changing pattern from {pattern} to {sp_pattern}");
                    var arg1 = &sp_pattern;
                    returnValue = this.addPatternHook!.OriginalFunction(self, other, returnValue, argc, &arg1);
                } else {
                    returnValue = this.addPatternHook!.OriginalFunction(self, other, returnValue, argc, argv);
                }
            } else {
                var sp_pattern = this.mpToSpMap[(pattern, difficulty)];
                this.LogDebug($"DEBUG: changing pattern from {pattern} to {sp_pattern}");
                var arg1 = &sp_pattern;
                returnValue = this.addPatternHook!.OriginalFunction(self, other, returnValue, argc, &arg1);
            }
        } else {
            returnValue = this.addPatternHook!.OriginalFunction(self, other, returnValue, argc, argv);
        }
        if (!this.isStartingCombat) {
            return returnValue;
        }
        var scriptId = (RValue) this.rnsReloaded.ScriptFindId("bp_nothing");
        var arg = &scriptId;
        this.addPatternHook!.OriginalFunction(self, other, returnValue, 1, &arg);
        return returnValue;
    }

    private RValue* BpswMultiDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.bpswMultiHook!.OriginalFunction(self, other, returnValue, argc, argv);
        // Args are [solo_thing, multi_thing], should return first if ST
        if (this.soloTogether != 0) {
            returnValue = argv[0];
        }
        return returnValue;
    }
    private RValue* BattleStartDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
    {
        this.isStartingCombat = true;
        returnValue = this.battleStartHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* MerranSlowDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv)
    {
        this.LogDebug("DEBUG: merran faceoff");
        foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
            foreach (var instance in layer->GetInstances()) {
                var val = instance["pingAver"];
                if (val != null && val->Type == RValueType.Array) {
                    this.LogDebug(val->ToString());
                }
            }
        }
        var clientNameArray = this.rnsReloaded.FindGlobalValue("clientName");
        this.LogDebug(clientNameArray->ToString());

        var speedR = this.rnsReloaded.FindGlobalValue("gameTimeSpeed");
        var savedSpeed = (double)*speedR;
        speedR->Real = 2.0; // Any value that merran script doesn't set (i.e. 1.0 or 0.3) will do
        returnValue = this.merranSlowHook!.OriginalFunction(self, other, returnValue, argc, argv);

        RValue inst = self;
        inst["pat_originalZoom"]->Type = RValueType.Real;
        inst["pat_originalZoom"]->Real = 0.75;

        // Pattern is ending, delete everything
        // if (speedR->Real == 1.0) {
        //     foreach (var layer in rnsReloaded.GetCurrentRoom()->Layers) {
        //         if (!layer->GetName().StartsWith("Pattern")) continue;
        //         this.LogDebug($"Script instances for layer {layer->GetName()}:");

        //         foreach (var instance in layer->GetInstances()) {
        //             this.LogDebug("  " + instance["actionScript"]->ToString() + $" , noPhaseErase = {instance["noPhaseErase"]->ToString()}");
        //             if (instance["actionScript"]->ToString().Contains("steeltooth1") && instance["noPhaseErase"] != null && !(bool)*instance["noPhaseErase"]) {
        //                 this.LogDebug($"    DEBUG: killing everything except: {instance["actionScript"]->ToString()}");
        //                 rnsReloaded.battleScripts.phase_pattern_remove(instance.Object, other);
        //                 rnsReloaded.battlePatterns.erase_patterns_network(instance.Object, other);
        //                 this.LogDebug($"    noPhaseErase after: {!(bool)*instance["noPhaseErase"]}");
        //             }
        //         }
        //     }
        // }

        // Check if speed was changed
        if (speedR->Real != 2.0) {
            // this.baseSpeed = (double)*speedR;
            this.LogDebug($"DEBUG: merran faceoff - detected changed speed");
            this.setSpeed(self, other, (double)*speedR, spawnDelay: 300);
        } else {
            speedR->Real = savedSpeed;
        }

        // Scripts saves current zoom on first of 3 runs when pattern starts, then sets zoom to that when pattern ends
        // Therefore we have to check for mult stacking
        // Im gonna let the case where this collides with the real zoom (1.6) be an easter egg. Zoom would need to be sqrt(1.6/0.75) = 1.460593487
        // var zoomR = rnsReloaded.FindGlobalValue("bfZoomTarget");
        // if (zoomR->Real == 0.75 * this.zoom * this.zoom) {
        //     this.setZoom(self, other, 0.75);
        // }

        return returnValue;
    }

    private RValue* ZoomDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.zoomHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (this.chaos == 0 && this.zoom != 1.0) {
            this.setZoom(self, other, (double)*argv[0]);
        }
        return returnValue;
    }

    private RValue* GamespeedDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.gamespeedHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (this.delayedSpeedchange) {
            this.LogDebug("Gamespeed was set by us");
            this.delayedSpeedchange = false;
            return returnValue;
        }
        var speedR = this.rnsReloaded.FindGlobalValue("gameTimeSpeed");
        this.setSpeed(self, other, (double)*speedR);
        return returnValue;
    }

    private RValue* GamespeedTempDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.gamespeedTempHook!.OriginalFunction(self, other, returnValue, argc, argv);
        var speedR = this.rnsReloaded.FindGlobalValue("gameTimeSpeed");
        this.setSpeed(self, other, (double)*speedR);
        return returnValue;
    }
    #endregion

    // Auto restart:
    // - when no active players
    // - when victory screen
    private RValue* GameframeDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        // this.LogDebug($"Gameframe!", Color.Gray);
        if (this.clientOwnId == null) {
            this.clientOwnId = this.rnsReloaded.FindGlobalValue("clientOwnId");
        }
        if (this.hostHooksEnabled && !this.isHost) {
            this.toggleHostHooks();
        } else if (!this.hostHooksEnabled && this.isHost) {
            this.toggleHostHooks();
        }

        returnValue = this.gameframeHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (!this.isHost) return returnValue;

        this.counterAfk--;
        if (this.counterAfk < 0) {
            this.counterAfk = 1000;
            if (!this.InfirstRoom()) {
                var playersArray = this.rnsReloaded.FindGlobalValue("player")->Get(0);
                var empty = true;
                for (var i = 0; i < 4; i++) {
                    var playerObj = playersArray->Get(i);
                    if (playerObj->Type != RValueType.Undefined && playerObj != null) {
                        i = 4;
                        empty = false;
                        break;
                    }
                }
                if (empty && this.config.AutoRestart) {
                    this.RestartGame();
                }
            }
        }
        if (this.restarting) {
            this.counter--;
            if (this.counter < 0) {
                this.rnsReloaded.ExecuteScript("scr_runmenu_victory_returnhost", self, other, []);
            }
        }
        return returnValue;
    }

    private RValue* GameRestartDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        this.restarting = false;
        this.counter = 300;
        returnValue = this.gameRestartHook!.OriginalFunction(self, other, returnValue, argc, argv);
        this.chatHook!.Enable();
        return returnValue;
    }

    private RValue* langStringDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        if (this.afkMessage != string.Empty && argc > 0 && argv[0]->ToString() == "chatsys_client_joined") {
            this.clientJoined = true;
        }
        return this.langStringHook!.OriginalFunction(self, other, returnValue, argc, argv);
    }

    #region chaos mode stuff
    private void ChaosMode(CInstance* self, CInstance* other, IRNSReloaded rnsReloaded) {
        if (rnsReloaded.battleScripts.time(self, other, 0)) {
            this.encounter++;
            this.mech = 0;
            // this.nextTime = 0;
            // this.nextMech = 0;
            // this.chaosZoom = 1;
            this.zoom = 1;
            // this.setZoom(self, other);
        }

        var startTime = 4000;
        var interval = 5000 / this.chaos;

        if (rnsReloaded.battleScripts.time_repeating(self, other, startTime, interval)) {
            this.LogDebug($"DEBUG: Chaos: doing mech {this.ints![this.encounter % 50][(this.mech+1) % 50]}");
            switch (this.ints![this.encounter % 50][this.mech++ % 50]) {
                case 0:
                    rnsReloaded.battlePatterns.gravity_fall_temporary(self, other, spawnDelay: 1000, eraseDelay: 5500, mult: 1.15);
                    break;  // Gravity down
                case 1:
                    rnsReloaded.battlePatterns.gravity_pull_temporary(self, other, spawnDelay: 1000, eraseDelay: 5000, position: (1920.0 / 2.0, 1080.0 / 2.0), mult: 1);
                    break;  // Gravity mid
                case 2:
                    rnsReloaded.battlePatterns.heavy_temporary(self, other, spawnDelay: 1000, hbsDuration: 4500);
                    break;  // Heavy
                case 3:
                    rnsReloaded.battlePatterns.heavyextra_temporary(self, other, spawnDelay: 1000, hbsDuration: 1000);
                    break;  // ExtraHeavy, short duration
                case 4:
                    double[] multArr = [1.0, 1.0, 1.0, 1.0, 2];
                    var mult = multArr[new Random().Next(multArr.Length)];
                    var length = 5500;
                    if (mult > 1.1) {
                        rnsReloaded.battlePatterns.apply_hbs_synced(self, other, delay: 1000, hbs: "hbs_super", hbsDuration: 4500, targetMask: 0b1111);
                    }
                    rnsReloaded.battlePatterns.tailwind(self, other, spawnDelay: 1000, eraseDelay: length, mult: mult, trgBinary: 127);
                    break;  // Tailwind, small chance of super + big wind
                case 5:
                    rnsReloaded.battlePatterns.movementcheck(self, other, spawnDelay: 5000, shouldMove: true, warningDelay: 1000, warnMsg: 2);
                    break;  // Keep moving
                case 6:
                    rnsReloaded.battlePatterns.movementcheck(self, other, spawnDelay: 5000, shouldMove: false, warningDelay: 1000, warnMsg: 2);
                    break;  // Stop moving
                case 7:
                    rnsReloaded.battlePatterns.setgamespeed_temp(self, other, spawnDelay: 1000, eraseDelay: 10000, timeMult: 1.5);
                    break;  // game speed up, takes 2 slots
                case 8:
                    rnsReloaded.battlePatterns.setgamespeed_temp(self, other, spawnDelay: 1000, eraseDelay: 4000, timeMult: 0.65);
                    break;  // game slow down
                case 9:
                    rnsReloaded.battlePatterns.apply_hbs_synced(self, other, delay: 1000, hbs: "hbs_nodefense", hbsDuration: 4000, targetMask: 0b1111);
                    break;  // No invul
                case 10:
                    //implement bullet rain
                    rnsReloaded.battlePatterns.light_line(self, other, warningDelay: 0, spawnDelay: 1500, position: (1940, -1000), lineAngle: 90, angle: 155, lineLength: 2500, numBullets: 15, type: 1, showWarning: true, spd: 9);
                    rnsReloaded.battlePatterns.light_line(self, other, warningDelay: 0, spawnDelay: 1500, position: (-20, -1000), lineAngle: 90, angle: 25, lineLength: 2500, numBullets: 15, type: 1, showWarning: true, spd: 9);
                    //too hard
                    if (new Random().NextDouble() < 0.5) {
                        rnsReloaded.battlePatterns.light_line(self, other, warningDelay: 0, spawnDelay: 3000, position: (1940, -1000), lineAngle: 90, angle: 155, lineLength: 2500, numBullets: 15, type: 1, showWarning: false, spd: 15);
                    } else {
                        rnsReloaded.battlePatterns.light_line(self, other, warningDelay: 0, spawnDelay: 3000, position: (-20, -1000), lineAngle: 90, angle: 25, lineLength: 2500, numBullets: 15, type: 1, showWarning: false, spd: 15);
                    }
                    break; // Bullets from side
                case 11:
                    rnsReloaded.battlePatterns.apply_hbs_synced(self, other, delay: 1000, hbs: "hbs_graniteskin", hbsDuration: 5000, targetMask: 0b1111);
                    break; // Graniteskin
                case 12:
                    rnsReloaded.battlePatterns.apply_hbs_synced(self, other, delay: 500, hbs: "hbs_stoneskin", hbsDuration: 3800, targetMask: 0b1111);
                    var playersArray = *rnsReloaded.FindGlobalValue("player")->Get(0);

                    double[] posX = [-1.0, -1.0, -1.0, -1.0];
                    double[] posY = [-1.0, -1.0, -1.0, -1.0];
                    for (var i = 0; i < 4; i++) {
                        if (playersArray[i] == null) { continue; }
                        var playerObj = *playersArray[i];
                        posX[i] = (double)*playerObj["distMovePrevX"];
                        posY[i] = (double)*playerObj["distMovePrevY"];
                    }

                    (double, double)[] offsets = [];
                    for (var i = 0; i < 4; i++) {
                        var next = (i + 1) % 4;
                        var offX = posX[next] - posX[i];
                        var offY = posY[next] - posY[i];
                        offsets = [..offsets, (offX, offY)];
                    }
                    rnsReloaded.battlePatterns.teleport_dist(self, other, spawnDelay: 1000, eraseDelay: 1000, type: 0, offsets: offsets);
                    break; // Stoneskin + teleport to other player
                case 13:
                    rnsReloaded.battlePatterns.apply_hbs_synced(self, other, delay: 1000, hbs: "hbs_abyssrage", hbsDuration: 4500, targetMask: 0b1111);
                    break; // AbyssRage
                case 14:
                    this.changeZoom(self, other, 1.6);
                    break; // Small arena
                case 15:
                    this.changeZoom(self, other, 0.5);
                    break; // Big arena
                case 16:
                    this.changeZoom(self, other, 1.0);
                    break; // Normal arena
                case 17:
                    if (new Random().NextDouble() < 0.5) {
                        rnsReloaded.battlePatterns.bind_h(self, other, warningDelay: 1000, spawnDelay: 4000, eraseDelay: 6000, targetMask: 127);
                    } else {
                        rnsReloaded.battlePatterns.bind_v(self, other, warningDelay: 1000, spawnDelay: 4000, eraseDelay: 6000, targetMask: 127);
                    }
                    break; // Binds

                default:
                    this.LogDebug($"DEBUG: Random mech was out of range");
                    break;

                    //ideas
                    //  thorns
                    //  spreads
                    //  field limit trap player
                    //  knockbacks
                    //  move the boss around

            }
        }

        // if (rnsReloaded.battleScripts.time(self, other, this.nextTime)) {
        //     this.LogDebug($"DEBUG: doing zoom stuff, nextMech={this.nextMech}, nextTime={this.nextTime}");
        //     switch (this.nextMech) {
        //         case 0:
        //             this.LogDebug($"DEBUG: Undoing zoom, was {this.zoom} now");
        //             if (this.zoom != 1.0) {
        //             }
        //             break;
        //         case 1:
        //             this.LogDebug($"DEBUG: Zooming in, had {this.zoom}, will get 1.6");
        //             this.changeZoom(self, other, 1.6);
        //             this.nextMech = 0;
        //             this.nextTime = startTime + interval * this.mech + 1000;
        //             rnsReloaded.battleScripts.time(self, other, this.nextTime);
        //             break; // Small arena
        //         case 2:
        //             this.LogDebug($"DEBUG: Zooming out, had {this.zoom}, will get 0.5");
        //             this.changeZoom(self, other, 0.5);
        //             this.nextMech = 0;
        //             this.nextTime = startTime + interval * this.mech + 1000;
        //             rnsReloaded.battleScripts.time(self, other, this.nextTime);
        //             break; // Big Arena
        //         default:
        //             break;
        //     }
        // }
    }
    private RValue* enemyHpDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        if (this.hpMult != 1.0) {
            this.LogDebug($"Multing HP, type was {argv[0]->Type}");
            argv[0]->Real = (double)*argv[0] * this.hpMult;
            argv[0]->Type = RValueType.Real;
        }
        returnValue = this.enemyHpHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }
    #endregion

    #region Solo together
    private RValue* resetEncsDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        this.mpToSpMap = [];
        returnValue = this.resetEncsHook!.OriginalFunction(self, other, returnValue, argc, argv);
        // try {
        //     dynamic gml = this.rnsReloaded.getGML();
        //     RValue diff = gml.ds_list_find_value(gml.global.saveInfo, 8);
        //     this.LogDebug(diff.ToString());
        // } catch (Exception e) {
        //     this.LogDebug(e.ToString());
        // }
        return returnValue;
    }

    private RValue* defEncDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.defEncHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (this.spPatterns.Count != this.mpPatterns.Count) {
            this.logger.PrintMessage($"Got different amounts of sp and mp patterns! sp {this.spPatterns.Count} != mp {this.mpPatterns.Count}", Color.Red);
            return returnValue;
        }
        for (var i = 0; i < this.spPatterns.Count; i++) {
            var mpPats = this.mpPatterns[i];
            var spPats = this.spPatterns[i];
            // Add pattern mapping for each difficulty
            for (var j = 0; j < 4; j++) {
                this.mpToSpMap.TryAdd((mpPats[j], j), spPats[j]);
            }
        }
        this.spPatterns = [];
        this.mpPatterns = [];
        return returnValue;
    }

    private RValue* spEncDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        var pat = *argv[2];
        this.spPatterns.Add([pat, pat, pat, pat]);
        returnValue = this.spEncHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* mpEncDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        var pat = *argv[2];
        this.mpPatterns.Add([pat, pat, pat, pat]);
        returnValue = this.mpEncHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* extraEncDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        var pat = *argv[4];
        var isMp = (bool)*argv[2];
        var difficulty = (int)*argv[3];
        if (isMp) {
            this.mpPatterns.Last()[difficulty] = pat;
        } else {
            this.spPatterns.Last()[difficulty] = pat;
        }
        returnValue = this.extraEncHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }
    #endregion

    private RValue* BpattVarDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        if (this.imSettingVar) {
            this.imSettingVar = false;
        } else {
            string name = "";
            for (var i = 0; i < argc; i++) {
                if (i%2 == 0) {
                    name = argv[i]->ToString();
                } else {
                    this.bpatt_var_local[name] = *argv[i];
                }
            }
        }

        returnValue = this.bpattVarHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;

    }

    private RValue* BpattVarResetDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        if (this.imResettingVar) {
            this.imResettingVar = false;
        } else {
            this.bpatt_var_local.Clear();
        }
        returnValue = this.bpattVarResetHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;

    }

    private RValue LocalVar(string name) {
        return this.bpatt_var_local.GetValueOrDefault(name, new RValue(0));
    }

    private RValue[] LocalVarToArgs() {
        RValue[] args = [];

        foreach (KeyValuePair<string, RValue> entry in this.bpatt_var_local) {
            args = [.. args, this.rnsReloaded.utils.CreateString(entry.Key)!.Value, entry.Value];
        }
        return args;
    }

    #region wip extra mechanics mode
    private RValue* BpattAddDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {

        if (this.imAddingMech) {
            this.imAddingMech = false;
            returnValue = this.bpattAddHook!.OriginalFunction(self, other, returnValue, argc, argv);
            return returnValue;
        } else {
            var currentMech = (int)argv[0];
            var parentMech = (int) this.rnsReloaded.FindValue(self, "actionScript");
            Console.WriteLine(Marshal.PtrToStringAnsi((nint) this.rnsReloaded.GetScriptData((int) this.rnsReloaded.FindValue(self, "actionScript") - 100000)->Functions->Name)!);
            Console.WriteLine(Marshal.PtrToStringAnsi((nint) this.rnsReloaded.GetScriptData(currentMech - 100000)->Functions->Name)!);
            Console.WriteLine();
            if (currentMech == this.rnsReloaded.ScriptFindId("bp_knockback_circle")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                RValue[] args = [];
                var isSpread = true;
                args =
                [
                    .. args,
                    this.rnsReloaded.utils.CreateString("spawnDelay")!.Value,
                    this.LocalVar("spawnDelay").ToInt(3000) + this.LocalVar("lifespan").ToInt(1000) + 750,
                ];
                if (parentMech == this.rnsReloaded.ScriptFindId("bp_wolf_steeltooth0_pt3") || parentMech == this.rnsReloaded.ScriptFindId("bp_rabbit_queen0_pt5")) {
                    isSpread = new Random().Next(2) > 0;
                }

                if (!isSpread) {
                    isSpread = false;
                    args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, 400];
                } else {
                    args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, 320 - (30 * this.rnsReloaded.utils.GetNumPlayers())];
                }

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);

                if (isSpread) {
                    args = [this.rnsReloaded.ScriptFindId("bp_circle_spreads")];
                } else {
                    args = [this.rnsReloaded.ScriptFindId("bp_prscircle_follow_bin")];
                }

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_knockback_line")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                RValue[] args = [];

                args =
                [
                    .. args,
                    this.rnsReloaded.utils.CreateString("spawnDelay")!.Value,
                    this.LocalVar("spawnDelay").ToInt(3000) + this.LocalVar("lifespan").ToInt(1000),
                ];
                args = [.. args, this.rnsReloaded.utils.CreateString("width")!.Value, 260 - (40 * this.rnsReloaded.utils.GetNumPlayers())];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);

                if ((int)this.LocalVar("horizontal") == 0) {
                    args = [this.rnsReloaded.ScriptFindId("bp_line_spreads_h")];
                } else {
                    args = [this.rnsReloaded.ScriptFindId("bp_line_spreads_v")];
                }
                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_colormatch")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                RValue[] args = [];

                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, this.LocalVar("spawnDelay").ToInt(3000) - 1700];
                args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, this.LocalVar("radius").ToInt(150) / 1.5];
                args = [.. args, this.rnsReloaded.utils.CreateString("warnMsg")!.Value, 2];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                args = [this.rnsReloaded.ScriptFindId("bp_circle_spreads")];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_colormatch2")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                RValue[] args = [];

                var shouldMove = new Random().Next(2);
                var spawnDelayExtra = 0;
                if (parentMech == this.rnsReloaded.ScriptFindId("bp_frog_idol1_pt4")) {
                    shouldMove = 1;
                    spawnDelayExtra = 750;
                }


                args = [.. args, this.rnsReloaded.utils.CreateString("shouldMove")!.Value, shouldMove];
                args =
                [
                    .. args,
                    this.rnsReloaded.utils.CreateString("spawnDelay")!.Value,
                    this.LocalVar("spawnDelay").ToInt(3000) + (shouldMove * 300) - 300 + spawnDelayExtra,
                ];
                args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, 100];
                args = [.. args, this.rnsReloaded.utils.CreateString("warnMsg")!.Value, 2];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                args = [this.rnsReloaded.ScriptFindId("bp_movementcheck")];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_colormatch3")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                var x = 0;
                while (true) {

                    if (this.LocalVar("posX_" + x).ToInt(1) != 1) {
                        x++;
                    } else {
                        break;
                    }
                }
                for (var i = 0; i < x; i++) {
                    var shouldMove = new Random().Next(2);
                    RValue[] args = [];
                    args = [.. args, this.rnsReloaded.utils.CreateString("shouldMove")!.Value, shouldMove];
                    args =
                    [
                        .. args,
                        this.rnsReloaded.utils.CreateString("spawnDelay")!.Value,
                        this.LocalVar("spawnDelay").ToInt(3000) + (shouldMove * 300) - 300 + (this.LocalVar("timeBetween").ToInt(3000) * i),
                    ];
                    args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, 100 + (i * 50)];
                    args = [.. args, this.rnsReloaded.utils.CreateString("warnMsg")!.Value, 2];

                    this.imSettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                    args = [this.rnsReloaded.ScriptFindId("bp_movementcheck")];

                    this.imAddingMech = true;
                    this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                    this.imResettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                    this.imSettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());
                }

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_movementcheck")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                RValue[] args = [];
                args = [.. args, this.rnsReloaded.utils.CreateString("shouldMove")!.Value, new RValue(this.LocalVar("shouldMove").ToInt(1) * -1 + 1)];
                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, new RValue(this.LocalVar("spawnDelay").ToInt(3000) + 750)];
                args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, new RValue(this.LocalVar("radius").ToInt(100) + 25)];
                args = [.. args, this.rnsReloaded.utils.CreateString("warnMsg")!.Value, new RValue(2)];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                args = [new RValue(this.rnsReloaded.ScriptFindId("bp_movementcheck"))];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_cleave_fixed")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                var x = 0;
                while (true) {

                    if (this.LocalVar("posX_" + x).ToInt(1) != 1) {
                        x++;
                    } else {
                        break;
                    }
                }
                for (int i = 0; i < x; i++) {
                    RValue[] args = [];
                    var coneCount = new Random().Next(1, 4);
                    args = [.. args, this.rnsReloaded.utils.CreateString("fanAngle")!.Value, new RValue(35 / coneCount)];
                    args = [.. args, this.rnsReloaded.utils.CreateString("x")!.Value, new RValue(1920 / 2)];
                    args = [.. args, this.rnsReloaded.utils.CreateString("y")!.Value, new RValue(1080 / 2)];
                    args = [.. args, this.rnsReloaded.utils.CreateString("numCones")!.Value, new RValue(coneCount)];

                    for (int c = 0; c < coneCount; c++) {
                        args =
                        [
                            .. args,
                            this.rnsReloaded.utils.CreateString("rot_" + c)!.Value,
                            new RValue(((int)this.LocalVar("rot_" + i) + 90 + (c + 1) * 180 / (coneCount + 1) + 360) % 360),
                        ];
                    }
                    this.imSettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                    args = [new RValue(this.rnsReloaded.ScriptFindId("bp_cone_direction"))];

                    this.imAddingMech = true;
                    this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);
                }
                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());
            }
            else if (currentMech == this.rnsReloaded.ScriptFindId("bp_cleave")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/

                if (parentMech == this.rnsReloaded.ScriptFindId("bp_wolf_steeltooth0_pt3") || parentMech == this.rnsReloaded.ScriptFindId("bp_wolf_snowfur0_pt3") || parentMech == this.rnsReloaded.ScriptFindId("bp_rabbit_queen0_pt5")) {//or shira's TODO, //if axe, maybe spread with 350 radius

                }else {
                    RValue[] args = [];
                    var isSpread = false;
                    if (parentMech == this.rnsReloaded.ScriptFindId("bp_queens_axe0")) {
                        isSpread = new Random().Next(0,2) == 0;
                    }
                    if (isSpread) {
                        args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, new RValue(380)];
                    } else {
                        args = [.. args, this.rnsReloaded.utils.CreateString("radius")!.Value, new RValue(300)];
                    }
                    args = [.. args, this.rnsReloaded.utils.CreateString("spd")!.Value, new RValue(20)];


                    this.imSettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);
                    if (parentMech == this.rnsReloaded.ScriptFindId("bp_queens_axe0")) {


                    }

                    if (isSpread) {
                        args = [new RValue(this.rnsReloaded.ScriptFindId("bp_circle_spreads"))];
                    } else {
                        args = [new RValue(this.rnsReloaded.ScriptFindId("bp_prscircle_follow_bin"))];
                    }

                    this.imAddingMech = true;
                    this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                    this.imResettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                    this.imSettingVar = true;
                    this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

                }
            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_clockspot")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                Console.WriteLine();

                RValue[] args = [];
                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, new RValue(this.LocalVar("spawnDelay").ToInt(3000) + 1000)];
                args = [.. args, this.rnsReloaded.utils.CreateString("warningDelay2")!.Value, new RValue(this.LocalVar("spawnDelay").ToInt(3000))];
                args = [.. args, this.rnsReloaded.utils.CreateString("fanAngle")!.Value, new RValue(this.LocalVar("fanAngle").ToInt(10) + 10)];

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                args = [new RValue(this.rnsReloaded.ScriptFindId("bp_clockspot"))];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_cone_spreads")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                Console.WriteLine();

                RValue[] args = [];
                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, new RValue(this.LocalVar("spawnDelay").ToInt(3000) + 1000)];
                args = [.. args, this.rnsReloaded.utils.CreateString("warningDelay2")!.Value, new RValue(this.LocalVar("spawnDelay").ToInt(3000))];
                args = [.. args, this.rnsReloaded.utils.CreateString("fanAngle")!.Value, new RValue(this.LocalVar("fanAngle").ToInt(10) + 10)];

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                args = [new RValue(this.rnsReloaded.ScriptFindId("bp_cone_spreads"))];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_fieldlimit_rectangle_temporary")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                Console.WriteLine();

                RValue[] args = [];


                args = [new RValue(this.rnsReloaded.ScriptFindId("bp_gravity_pull_temporary"))];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());


            }
            else if (currentMech == this.rnsReloaded.ScriptFindId("bp_fieldlimit_rectangle")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/
                Console.WriteLine();

                RValue[] args = [];


                args = [new RValue(this.rnsReloaded.ScriptFindId("bp_gravity_pull"))];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            }
            else if (currentMech == this.rnsReloaded.ScriptFindId("bp_bind_v")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/

                var randomDir = new Random().Next(0, 2) * -2 + 1;
                var randomLength = 400;//new Random().Next(300, 700);
                RValue[] args = [];
                args = [.. args, this.rnsReloaded.utils.CreateString("eraseDelay")!.Value, (int)this.LocalVar("spawnDelay")];
                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, new RValue(0)];
                args = [.. args, this.rnsReloaded.utils.CreateString("offX")!.Value, new RValue(randomLength * randomDir)];
                args = [.. args, this.rnsReloaded.utils.CreateString("offY")!.Value, new RValue(0)];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);

                if (parentMech != this.rnsReloaded.ScriptFindId("bp_mouse_oakspear1")) {

                    args = [new RValue(this.rnsReloaded.ScriptFindId("bp_teleport_bin"))];

                    this.imAddingMech = true;
                    this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);
                }

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_bind_h")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/

                var randomDir = new Random().Next(0, 2) * -2 + 1;
                var randomLength = 400;//new Random().Next(300, 700);
                RValue[] args = [];
                args = [.. args, this.rnsReloaded.utils.CreateString("eraseDelay")!.Value, (int)this.LocalVar("spawnDelay")];
                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, new RValue(0)];
                args = [.. args, this.rnsReloaded.utils.CreateString("offX")!.Value, new RValue(0)];
                args = [.. args, this.rnsReloaded.utils.CreateString("offY")!.Value, new RValue(randomLength * randomDir)];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                if (parentMech != this.rnsReloaded.ScriptFindId("bp_bird_valedictorian0_pt3_h")) {
                    args = [new RValue(this.rnsReloaded.ScriptFindId("bp_teleport_bin"))];

                    this.imAddingMech = true;
                    this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);
                }

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_prsline_v_follow") || currentMech == this.rnsReloaded.ScriptFindId("bp_prsline_h_follow")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/

                if (parentMech == this.rnsReloaded.ScriptFindId("bp_bird_whispering1") ||  parentMech == this.rnsReloaded.ScriptFindId("bp_bird_valedictorian0_pt6_h")) {

                    RValue[] args = [];
                    Console.WriteLine("delaying");
                    args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, (int)this.LocalVar("spawnDelay") + 750];

                    this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);
                }

            } else if (currentMech == this.rnsReloaded.ScriptFindId("bp_bind_h")) {
                /*this.imResettingVar = true;
                rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);*/

                var randomDir = new Random().Next(0, 2) * -2 + 1;
                var randomLength = 400;//new Random().Next(300, 700);
                RValue[] args = [];
                args = [.. args, this.rnsReloaded.utils.CreateString("eraseDelay")!.Value, (int)this.LocalVar("spawnDelay")];
                args = [.. args, this.rnsReloaded.utils.CreateString("spawnDelay")!.Value, new RValue(0)];
                args = [.. args, this.rnsReloaded.utils.CreateString("offX")!.Value, new RValue(0)];
                args = [.. args, this.rnsReloaded.utils.CreateString("offY")!.Value, new RValue(randomLength * randomDir)];


                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, args);


                args = [new RValue(this.rnsReloaded.ScriptFindId("bp_teleport_bin"))];

                this.imAddingMech = true;
                this.rnsReloaded.ExecuteScript("bpatt_add", self, other, args);

                this.imResettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var_reset", self, other, []);

                this.imSettingVar = true;
                this.rnsReloaded.ExecuteScript("bpatt_var", self, other, this.LocalVarToArgs());

            }
        }
        /*
        thorns = add get into the circle

        chains =

        teleport = add stop moving b4

        snowfur pt6 impossibe
        pt5 make circle bigger
        cap radius of movement check


        line h/v = ray in the middle

        spinning ray = cone spread


        solo spread replacement:

        circle knockback = bp_fire2_blast

        line knockback = light line

         */


        returnValue = this.bpattAddHook!.OriginalFunction(self, other, returnValue, argc, argv);
                return returnValue;

            }
    #endregion

    #region utils
    public static string readAsUtf8(string original) {
        Encoding encoding = Encoding.Default;

        if (encoding == Encoding.UTF8)
            return original;

        byte[] encBytes = encoding.GetBytes(original);
        byte[] utf8Bytes = Encoding.Convert(encoding, Encoding.UTF8, encBytes);
        return Encoding.UTF8.GetString(utf8Bytes);
    }

    private void LogDebug(string message, Color? color = null) {
        if (!this.config.DebugMode) { return; }

        if (color != null) {
            this.logger.PrintMessage(message, color.Value);
        } else {
            Console.WriteLine(message);
        }
    }

    // Self and other needs to be an instance for a pattern, i.e. this can only be run from within another pattern
    private void setZoom(CInstance* self, CInstance* other, double baseZoom = 1.0) {
        this.LogDebug($"DEBUG: Changing zoom to {baseZoom} * {this.zoom}");
        this.zoomHook!.Disable();
        this.rnsReloaded.battlePatterns.setzoom(self, other, baseZoom * this.zoom);
        this.zoomHook!.Enable();
    }

    private void changeZoom(CInstance* self, CInstance* other, double newZoom) {
        var baseZoom = (double)*this.rnsReloaded.FindGlobalValue("bfZoomTarget");
        this.LogDebug($"DEBUG: zoom was {this.zoom}, setting to {newZoom}. Total zoom atm: {baseZoom}");
        baseZoom /= this.zoom;
        this.zoom = newZoom;
        this.LogDebug($"DEBUG: Changing zoom to {baseZoom} * {newZoom}");
        this.setZoom(self, other, baseZoom);
    }

    // Self and other needs to be an instance for a pattern, i.e. this can only be run from within another pattern
    private void setSpeed(CInstance* self, CInstance* other, double baseSpeed = 1.0, int? spawnDelay = null) {
        var instR = (RValue)self;
        this.LogDebug($"DEBUG: Changing speed to {baseSpeed} * {this.speed} in {*instR["actionScript"]}");
        if (spawnDelay != null) {
            // When gamespeed script (again) runs in spawnDelay ms, ignore
            this.delayedSpeedchange = true;
        }
        this.gamespeedHook!.Disable();
        this.rnsReloaded.battlePatterns.setgamespeed(self, other, spawnDelay: spawnDelay, timeMult: baseSpeed * this.speed);
        this.gamespeedHook!.Enable();
    }

    private void toggleHostHooks(bool? host = null) {
        this.hostHooksEnabled = host ?? !this.hostHooksEnabled;
        var turnedTo = this.hostHooksEnabled ? "on" : "off";
        this.LogDebug($"Turned host-only hooks {turnedTo}", Color.Orange);

        // Enable/Disable host-only hooks
        if (this.hostHooksEnabled) {
            this.chatHook!.Enable();
            this.zoomHook!.Enable();
            this.gameOverHoook!.Enable();
            this.addPatternHook!.Enable();
            this.randomHook!.Enable();
            this.merranSlowHook!.Enable();
            this.battleStartHook!.Enable();
            this.nothingHook!.Enable();
            this.gamespeedHook!.Enable();
            this.gamespeedTempHook!.Enable();
            // this.extraHook!.Enable();
            this.langStringHook!.Enable();
        } else {
            this.chatHook!.Disable();
            this.zoomHook!.Disable();
            this.gameOverHoook!.Disable();
            this.addPatternHook!.Disable();
            this.randomHook!.Disable();
            this.merranSlowHook!.Disable();
            this.battleStartHook!.Disable();
            this.nothingHook!.Disable();
            this.gamespeedHook!.Disable();
            this.gamespeedTempHook!.Disable();
            // this.extraHook!.Disable();
            this.langStringHook!.Disable();
        }
    }
    #endregion

    #region gameover logging to console
    private RValue* GameOverDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        if (this.config.AutoRestart) {
            this.chatHook!.Disable();
            this.counter = 300;
            this.restarting = true;
        }

        var currentPos = new RValue();
        var hallwayPos = new RValue();
        var hallkey = new RValue();
        int[] clientId = [-1, -1, -1, -1];
        try {
            var playersArray = this.rnsReloaded.FindGlobalValue("player")->Get(0);
            for (var i = 0; i < 4; i++) {
                var playerObj = playersArray->Get(i);
                if (playerObj->Type == RValueType.Undefined || playerObj == null) {
                    continue;
                }
                foreach (var key in this.rnsReloaded.GetStructKeys(playerObj)) {
                    var val = this.rnsReloaded.FindValue(playerObj->Object, key);
                    if (key == "clientId") {
                        clientId[i] = val->ToInt(-1);
                    }
                }
            }

            foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
                if (layer->GetName() != "RunMenu_Blocker") { continue; }
                foreach (var instance in layer->GetInstances()) {
                    foreach (var key in this.rnsReloaded.GetStructKeys(&instance)) {
                        var val = this.rnsReloaded.FindValue(instance.Object, key);
                        if (key == "currentPos") {
                            currentPos = *val;
                        }
                        if (key == "hallwayPos") {
                            hallwayPos = *val;
                        }
                        if (key == "hallkey") {
                            hallkey = *val;
                        }
                    }
                }
            }

            var difficulty = this.rnsReloaded.ExecuteScript("scr_diffswitch", self, other,
                [this.rnsReloaded.utils.CreateString("Cute")!.Value,
                this.rnsReloaded.utils.CreateString("Normal")!.Value,
                this.rnsReloaded.utils.CreateString("Hard")!.Value,
                this.rnsReloaded.utils.CreateString("Lunar")!.Value])!.Value;
            var playersNameArray = this.rnsReloaded.FindGlobalValue("playerName")->Get(0);
            var clientNameArray = this.rnsReloaded.FindGlobalValue("clientName");
            this.logger.PrintMessage("~~~Finish run~~~", this.logger.ColorYellowLight);
            Console.WriteLine("Difficulty: " + difficulty.ToString());
            Console.WriteLine(this.CurrentString());
            Console.WriteLine("Stage rng: " + hallkey.ToString());
            Console.WriteLine("Finish on: " + hallkey.Get((int)hallwayPos)->ToString());
            Console.WriteLine("Finished on room: " + currentPos.ToString());
            Console.Write("Players: ");
            for (var i = 0; i < 4; i++) {
                if (clientId[i] != -1) {
                    Console.Write(playersNameArray->Get(i)->ToString() + "(" + clientNameArray->Get(clientId[i])->ToString() + ")");
                    if (i < 3) {
                        Console.Write(", ");
                    } else {
                        Console.WriteLine();
                    }
                }
            }
            Console.WriteLine("In room: " + clientNameArray->ToString());
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
        // clientName in globals
        // playerName
        // clientId in player arr
        // hallwayPos

        return this.gameOverHoook!.OriginalFunction(self, other, returnValue, argc, argv);
    }
    #endregion

    #region Chat stuff
    private RValue* ChatDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        returnValue = this.chatHook!.OriginalFunction(self, other, returnValue, argc, argv);
        if (argc <= 3) {
            this.LogDebug("Chat function had no message");
            return returnValue;
        }
        var clientId = (int)*argv[0];
        var shouldCheckAndIsHost = !this.config.HostOnly || clientId == 0;
        var clientNameArray = *this.rnsReloaded.FindGlobalValue("clientName");
        var clientName = clientNameArray[Math.Clamp(clientId, 0, 11)]->ToString();
        var msg = argv[3]->ToString();
        Console.WriteLine(clientName + ": " + readAsUtf8(msg));
        this.LogDebug($"argv[0] = {argv[0]->ToString()}, argv[1] = {argv[1]->ToString()}, argv[2] = {argv[2]->ToString()}, argv[3] = {argv[3]->ToString()}");
        if (this.afkMessage != "" && this.clientJoined) {
            this.clientJoined = false;
            this.systemPrint(self, other, this.rnsReloaded, this.afkMessage);
            this.rnsReloaded.ExecuteScript("scr_lobbyhost_call_all", self, other, [5]);
        }
        if (msg == "!help" || msg == "!help 1") {
            this.systemPrint(self, other, this.rnsReloaded, "Waiting area or current players:");
            this.systemPrint(self, other, this.rnsReloaded, " !restart , restarts run |  !reset , resets current settings");
            this.systemPrint(self, other, this.rnsReloaded, " !heavy 0-2 , 0 off, 1 heavy, 2 very heavy");
            this.systemPrint(self, other, this.rnsReloaded, " !grav 0-10 !wind 0-10 , 0 is off, 1 is normal, >2 not recommended");
            this.systemPrint(self, other, this.rnsReloaded, "Use <!help 2> for next page");
        } else if (msg == "!help2" || msg == "!help 2") {
            this.systemPrint(self, other, this.rnsReloaded, " !speed 0.2-10 , 1 is off, recommended 1.5");
            this.systemPrint(self, other, this.rnsReloaded, " !bh 0-20 , Bullet hell. 0 is off, recommended <10 unlesss low diff or defenders");
            this.systemPrint(self, other, this.rnsReloaded, " !zoom 0.1-2 , 1 is off, recommended to lower for some bosses like Karsi");
            this.systemPrint(self, other, this.rnsReloaded, " !chaos 0-10 , Chaos mode! Will disable all other settings");
            this.systemPrint(self, other, this.rnsReloaded, "Use <!help 3> for next page");
        } else if (msg == "!help3" || msg == "!help 3") {
            this.systemPrint(self, other, this.rnsReloaded, " !st [0-1] , Turn on SoloTogether. Argument sets chance of getting solo pattern");
            this.systemPrint(self, other, this.rnsReloaded, " !hp 0.01-100 , Multiply enemy HP. Clients get wrong max HP but correct current HP");
            this.systemPrint(self, other, this.rnsReloaded, " !cur , Shows current mode, !ping shows your latency to host, !pingall shows all pings");
            this.systemPrint(self, other, this.rnsReloaded, " !afk <msg> , Sets a message that will be posted when someone joins");
            this.systemPrint(self, other, this.rnsReloaded, "Use <!help 4> for next page");
        } else if (msg == "!help4" || msg == "!help 4") {
            this.systemPrint(self, other, this.rnsReloaded, "Only in waiting area:");
            this.systemPrint(self, other, this.rnsReloaded, " !dest 0-11 , Random, Bird, Wolf, Dragon, Mice, Frog, Depths, Sanctum, Aurum, Random Extra, True Random, Chaotic Random");
            this.systemPrint(self, other, this.rnsReloaded, " !diff 0-3  Cute, Normal, Hard, Lunar");
        } else if (msg.StartsWith("!cur")) {
            this.printCurrent(self, other, this.rnsReloaded);
        } else if (msg.StartsWith("!reset") && shouldCheckAndIsHost) {
            this.speed = DefaultSpeed;
            this.bulletHell = DefaultBulletHell;
            this.zoom = DefaultZoom;
            this.winds = DefaultWinds;
            this.grav = DefaultGrav;
            this.heavy = DefaultHeavy;
            this.chaos = DefaultChaos;
            this.soloTogether = DefaultSoloTogether;
            this.hpMult = DefaultHpMult;
            this.printCurrent(self, other, this.rnsReloaded);
        } else if (msg == "!ping") {
            try {
                var ping = new RValue();
                foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
                    foreach (var instance in layer->GetInstances()) {
                        var val = instance["pingAver"];
                        if (val != null && val->Type == RValueType.Array) {
                            this.LogDebug(instance.ToString());
                            ping = *val;
                        }
                    }
                }
                this.LogDebug(clientId.ToString());
                if (clientId > 0) {
                    this.systemPrint(self, other, this.rnsReloaded, ping[clientId]->ToString() + "ms");
                } else {
                    this.systemPrint(self, other, this.rnsReloaded, "0ms");
                }
            } catch (Exception e) {
                this.LogDebug(e.ToString());
                this.LogDebug("!ping failed with above exception");
            }
        } else if (msg == "!pingall") {
            try {
                var pings = new RValue([]);
                foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
                    foreach (var instance in layer->GetInstances()) {
                        var val = instance["pingAver"];
                        if (val != null && val->Type == RValueType.Array) {
                            pings = *val;
                        }
                    }
                }
                this.systemPrint(self, other, this.rnsReloaded, "Clients' ping to host:");
                this.systemPrint(self, other, this.rnsReloaded, $"{clientNameArray[0]->ToString()} : 0ms");
                for (var i = 1; i < pings.ArrayLength(); i++) {
                    var name = clientNameArray[i]->ToString();
                    if (name == string.Empty || (int)*pings[i] == 150) continue;
                    this.systemPrint(self, other, this.rnsReloaded, $"{name} : {pings[i]->ToString()}ms");
                }
            } catch (Exception e) {
                this.LogDebug(e.ToString());
                this.LogDebug("!pingall failed with above exception");
            }
        } else if (msg == "!restart" && this.config.AutoRestart && shouldCheckAndIsHost) {
            if (this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                this.RestartGame();
            }
        } else if (msg.StartsWith("!afk") && shouldCheckAndIsHost) {
            this.afkMessage = msg["!afk".Length..];
            this.systemPrint(self, other, this.rnsReloaded, "Changed AFK message");
        } else if (msg.StartsWith("!dest ") && this.InfirstRoom() && this.config.AutoRestart && shouldCheckAndIsHost) {
            var a = msg.Split(' ');
            if (a.Length > 1) {
                try {
                    var b = int.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                    if (0 < b && b < 12) {
                        this.rnsReloaded.ExecuteScript("scr_stagefirst_change", self, other, [b]);
                        // this.rnsReloaded.ExecuteScript("scr_stagefirst_diffdisp_sync", self, other, []);
                        // this.rnsReloaded.ExecuteScript("scr_stagefirst_mapdeco_sync", self, other, []);
                        // TODO: needs testing to see if restart can be avoided
                        // this.rnsReloaded.ExecuteScript("scr_lobbyhost_call_all", self, other, [12, true]);
                        // this.rnsReloaded.ExecuteScript("scr_lobbyhost_call_all", self, other, [13]);
                        this.RestartGame();
                    }
                } catch (Exception e) {
                    this.LogDebug($"{e}");
                    this.LogDebug($"Failed to parse {a[1]} into int");
                }
            }
        } else if (msg.StartsWith("!diff ") && this.InfirstRoom() && this.config.AutoRestart && shouldCheckAndIsHost) {
            var a = msg.Split(' ');
            if (a.Length > 1) {
                try {
                    var b = int.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                    if (0 <= b && b < 4) {
                        this.rnsReloaded.ExecuteScript("scr_difficulty_change", self, other, [b]);
                        // TODO: needs testing to see if restart can be avoided
                        // this.rnsReloaded.ExecuteScript("scr_lobbyhost_call_all", self, other, [12, true]);
                        // this.rnsReloaded.ExecuteScript("scr_lobbyhost_call_all", self, other, [13]);
                        // this.rnsReloaded.ExecuteScript("scr_steamcb_lobby_update_metadata", self, other, []);
                        this.RestartGame();
                    }
                } catch (Exception e) {
                    this.LogDebug($"{e}");
                    this.LogDebug($"Failed to parse {a[1]} into int");
                }
            }
        } else if (msg.StartsWith("!wind") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b < 11.0) {
                            this.winds = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                }
            }
        } else if (msg.StartsWith("!grav") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b < 11.0) {
                            this.grav = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                }
            }
        } else if (msg.StartsWith("!heavy") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = int.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b >= 0 && b < 3) {
                            this.heavy = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into int");
                    }
                }
            }
        } else if (msg.StartsWith("!speed") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b > 0.1 && b < 11.0) {
                            this.speed = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                }
            }
        } else if (msg.StartsWith("!bh") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b >= -1 && b < 21) {
                            var c = b * 250.0;
                            this.bulletHell = (int) c;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                }
            }
        } else if (msg.StartsWith("!zoom") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b > 0.05 && b < 11.0) {
                            this.zoom = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                }
            }
        } else if (msg.StartsWith("!chaos") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = int.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b >= 0 && b <= 10) {
                            this.chaos = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into int");
                    }
                }
            }
        } else if (msg.StartsWith("!st") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b >= 0 && b <= 1) {
                            this.soloTogether = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                } else {
                    // Toggle solo together
                    if (this.soloTogether > 0) {
                        this.soloTogether = 0;
                    } else {
                        this.soloTogether = 1;
                    }
                    this.printCurrent(self, other, this.rnsReloaded);
                }
            }
        } else if (msg.StartsWith("!hp") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = double.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b >= 0.01 && b <= 100) {
                            this.hpMult = b;
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into double");
                    }
                }
            }
        } else if (msg.StartsWith("!wipmode") && shouldCheckAndIsHost) {
            if (argv[0]->ToString() == "0" || this.InfirstRoom() || argv[1]->ToString() == "0" || argv[1]->ToString() == "1" || argv[1]->ToString() == "2" || argv[1]->ToString() == "3") {
                var a = msg.Split(' ');
                if (a.Length > 1) {
                    try {
                        var b = int.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture);
                        if (b == 0) {
                        this.bpattVarHook!.Disable();
                        this.bpattVarResetHook!.Disable();
                        this.bpattAddHook!.Disable();
                        this.printCurrent(self, other, this.rnsReloaded);
                        } else {

                            this.bpattVarHook!.Enable();
                            this.bpattVarResetHook!.Enable();
                            this.bpattAddHook!.Enable();
                            this.printCurrent(self, other, this.rnsReloaded);
                        }
                    } catch (Exception) {
                        this.LogDebug($"Failed to parse {a[1]} into int");
                    }
                }
            }
        } else if (msg.StartsWith("!test") && shouldCheckAndIsHost) {
            try {
                // var saveInfo = *this.rnsReloaded.FindGlobalValue("saveInfo");
                // var difficulty = (int)*saveInfo[];
                // this.LogDebug($"Diff: {difficulty}");
                // foreach (var layer in rnsReloaded.GetCurrentRoom()->Layers) {
                //     foreach (var instance in layer->GetInstances()) {
                //         var val = instance["clientId"];
                //         if (val == null) continue;
                //         this.LogDebug(val->ToString());
                //         this.LogDebug($"clientId, instance: Layer {instance.Object->layerID}, instance {instance.Object->ID}");
                //     }
                // }
                // foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
                //     foreach (var instance in layer->GetInstances()) {
                //         var val = instance["pingAver"];
                //         if (val == null) continue;
                //         this.LogDebug(instance.ToString());
                //         this.LogDebug($"ping instance: Layer {instance.Object->layerID}, instance {instance.Object->ID}");
                //     }
                // }

                // var playersArray = *this.rnsReloaded.FindGlobalValue("player")->Get(0);
                // foreach (var player in playersArray.AsSpan()) {
                //     if (player.Type == RValueType.Undefined) continue;
                //     this.LogDebug("Player: " + player.ToString());
                //     this.LogDebug("Client ID: " + player["clientId"]->ToString());
                // }
                // var clientNames = *this.rnsReloaded.FindGlobalValue("clientName");
                // var playerNames = *this.rnsReloaded.FindGlobalValue("playerName")->Get(0);
                // this.LogDebug("Client names: " + clientNames);
                // this.LogDebug("Player names: " + playerNames.ToString());
                // this.LogDebug(playerNames.Type.ToString());
                // var clientMap = *this.rnsReloaded.FindGlobalValue("clientMap");
                // var keys = this.rnsReloaded.ExecuteCodeFunction("ds_map_keys_to_array", null, null, [clientMap]) ?? new RValue([]);
                // var values = this.rnsReloaded.ExecuteCodeFunction("ds_map_values_to_array", null, null, [clientMap]) ?? new RValue([]);
                // this.LogDebug("Client map keys");
                // foreach (var key in keys.AsSpan()) {
                //     this.LogDebug(key.ToString());
                // }
                // this.LogDebug("values");
                // foreach (var value in values.AsSpan()) {
                //     this.LogDebug(value.ToString());
                // }
            } catch (Exception e) {
                this.LogDebug(e.ToString());
            }
            // TODO: remove
            // this.LogDebug("Running test command");
            // foreach (var layer in rnsReloaded.GetCurrentRoom()->Layers) {
            //     foreach (var el in layer->Elements) {
            //         var el_name = el->Name == null ? "Unnamed" : Marshal.PtrToStringAnsi((nint) el->Name)!;
            //         if (el->Type == LayerElementType.Instance) {
            //             var inst = (CLayerInstanceElement*) el;
            //             this.LogDebug($"El: {el->ID}, inst_el: {inst->InstanceID}, inst: {inst->Instance->ID}, name: {el_name}");
            //             this.LogDebug($"Layer: {layer->ID}, el: {el->Layer->ID}, inst: {inst->Instance->layerID}, name: {layer->GetName()}");
            //         } else {
            //             this.LogDebug($"El: {el->ID}, name: {el_name}");
            //             this.LogDebug($"Layer: {layer->ID}, el: {el->Layer->ID}, name: {layer->GetName()}");
            //         }
            //     }
            // }
            // var bytes = (byte*)self;
            // for (var i = 0; i < 512; i++) {
            //     Console.Write(bytes[i].ToString("X2"));
            //     Console.Write(' ');
            // }
            // Console.WriteLine();
            // this.LogDebug(self->ID.ToString());
        }
        return returnValue;
    }
    #endregion

    // carry over from non-host-only's chaos mode
    // using map seed so all clients does the same mechanic
    private RValue* MakeRandomDetour(CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv) {
        //scr_enemy_get_defeated
        //scr_hallwayprogress_choose_halls

        this.encounter = 0;
        var mapSeedR = this.rnsReloaded.FindGlobalValue("mapSeed");
        var mapSeed = (int)mapSeedR; // mapSeed is a different datatype for host/client

        this.random = new Random(mapSeed);
        this.ints = new int[50][];
        for (int i = 0; i < 50; i++) {
            var a = new int[50];
            for (int j = 0; j < 50; j++) {
                a[j] = this.random.Next(0, 18);
            }
            this.ints[i] = a;
        }
        return this.randomHook!.OriginalFunction(self, other, returnValue, argc, argv);
    }

    private string CurrentString() {
        var str = "Current changed settings: ";
        var isDefault = true;
        if (this.chaos != DefaultChaos) {
            isDefault = false;
            str += $"Chaos: {this.chaos}, ";
        }
        if (this.speed != DefaultSpeed) {
            isDefault = false;
            str += $"Speed: {this.speed}, ";
        }
        if (this.zoom != DefaultZoom) {
            isDefault = false;
            str += $"Zoom: {this.zoom}, ";
        }
        if (this.winds != DefaultWinds) {
            isDefault = false;
            str += $"Winds: {this.winds}, ";
        }
        if (this.grav != DefaultGrav) {
            isDefault = false;
            str += $"Grav: {this.grav}, ";
        }
        if (this.heavy != DefaultHeavy) {
            isDefault = false;
            str += $"Heavy: {this.heavy}, ";
        }
        if (this.bulletHell != DefaultBulletHell) {
            isDefault = false;
            str += $"bh: {this.bulletHell / 250}, ";
        }
        if (this.soloTogether != DefaultSoloTogether) {
            isDefault = false;
            str += $"ST: {this.soloTogether}, ";
        }
        if (this.hpMult != DefaultHpMult) {
            isDefault = false;
            str += $"HP: {this.hpMult}, ";
        }
        if (isDefault) {
            str += "All default";
        } else {
            str = str[..(str.Count() - 2)];
        }
        // return "Current Settings: Chaos: " + this.chaos + ", Zoom: " + this.zoom + ", Speed: " + this.speed + ", bh: " + this.bulletHell / 250 + ", Wind: " + this.winds + ", Grav: " + this.grav + ", Heavy: " + this.heavy + ", ST: " + this.soloTogether + ", HP: " + this.hpMult;
        return str;
    }

    private void printCurrent(CInstance* self, CInstance* other, IRNSReloaded rnsReloaded) {
        rnsReloaded.ExecuteScript("scr_chat_add_mesage_system", self, other, [rnsReloaded.utils.CreateString(this.CurrentString())!.Value]);
    }

    private void systemPrint(CInstance* self, CInstance* other, IRNSReloaded rnsReloaded, string a) {
        rnsReloaded.ExecuteScript("scr_chat_add_mesage_system", self, other, [rnsReloaded.utils.CreateString(a)!.Value]);
    }

    private bool InfirstRoom() {
        var currentPos = new RValue();
        var stageKey = new RValue();
        foreach (var layer in this.rnsReloaded.GetCurrentRoom()->Layers) {
            var name = layer->Name == null ? "Unnamed" : Marshal.PtrToStringAnsi((nint) layer->Name)!;
            if (name != "RunMenu_Blocker") { continue; }
            foreach (var instance in layer->GetInstances()) {
                foreach (var key in this.rnsReloaded.GetStructKeys(&instance)) {
                    var val = this.rnsReloaded.FindValue(instance.Object, key);
                    if (key == "currentPos") {
                        currentPos = *val;
                    }
                    if (key == "stageKey") {
                        stageKey = *val;
                    }
                }
            }
        }
        return (currentPos.ToString() == "-1" || currentPos.ToString() == "0") // In first room of area
            && (stageKey.ToString() == "1" || stageKey.ToString() == "9"); // Area is outskirts or geode
    }

    private void RestartGame() {
        this.rnsReloaded.ExecuteScript("scr_runmenu_giveup_giveup", null, null, []);
    }

    public void Resume() { }
    public void Suspend() { }
    public bool CanSuspend() => true;

    public void Unload() { }
    public bool CanUnload() => false;

    public Action Disposing => () => { };
}
