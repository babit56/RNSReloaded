using System.Xml.Linq;
using Reloaded.Hooks.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using RNSReloaded.Interfaces;
using RNSReloaded.Interfaces.Structs;
using RNSReloaded.Inspecting.Config;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Data;
using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using RNSReloaded.Inspecting.Structs;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Drawing;

namespace RNSReloaded.Inspecting;

public delegate double RandomDelegate(double upper);
public delegate double RandomRangeDelegate(double lower, double upper);
public delegate double IRandomDelegate(long upper);
public delegate double IRandomRangeDelegate(long lower, long upper);

public unsafe class Mod : IMod {
    private WeakReference<IRNSReloaded>? rnsReloadedRef;
    private WeakReference<IReloadedHooks>? hooksRef;
    private WeakReference<IStartupScanner>? scannerRef;
    private ILoggerV1 logger = null!;

    private Configurator configurator = null!;
    private Config.Config config = null!;

    private IHook<ScriptDelegate>? gameSpeedHook;
    private IHook<ScriptDelegate>? encounterStartHook;
    private IHook<ScriptDelegate>? chooseHallsHook;
    private IHook<RoutineDelegate>? arrayShuffleHook;
    private IHook<RoutineDelegate>? arrayShuffleExtHook;
    private IHook<RoutineDelegate>? dsListShuffleHook;
    private IHook<RoutineDelegate>? dsListCopyHook;
    private IHook<RoutineDelegate>? randomSetSeedHook;
    private IHook<RoutineDelegate>? chooseHook;
    private IHook<RandomDelegate>? randomHook;
    private IHook<RandomRangeDelegate>? randomRangeHook;
    private IHook<IRandomDelegate>? irandomHook;
    private IHook<IRandomRangeDelegate>? irandomRangeHook;
    private Dictionary<String, IHook<ScriptDelegate>> hookMap = [];
    private Dictionary<String, IHook<RoutineDelegate>> hookFuncMap = [];
    private RValue* itemData;
    private RValue* enemyData;
    private long mapSeedR;
    private long dumpIndex = 0;
    private Dictionary<string, string> globals = [];
    private int depth = 0;
    // Definitions stuff. Recursively retrieved from dataloader_Create and stagefirst resp.
    private readonly List<string> dumpScripts = [
        "scrdtit_polar_coat", "scrdtmv_druid_2_defstat", "scrdtit_wolf_hood", "scrdtit_vorpal_dao", "scrdtit_hermes_bow", "scrdtit_nightguard_gloves", "scrdtpln_line", "screnc_data_pattern_ext", "scrdtit_midsummer_dress", "scrdtit_hydrous_blob", "scrdtit_clay_rabbit", "scranim_data_set_external_simple", "scrdtmv_gunner_3_deftrig", "scrdtmv_hammer_3_deftrig", "scrdtit_raven_grimoire", "scrdtit_phantom_dagger", "scrdtit_stormdance_gown", "scrdtit_sun_pendant", "scrpj_bullet_data_row", "scranim_data_headoffx", "scrdt_dialog_entry", "scrdt_quest_deco", "scrdtit_dynamite_staff", "scrdtmv_spsword_3_deftrig", "scrdtmv_sniper_1_defstat", "scrdtenc_streets", "scr_readsheet_populate_possible_halls", "scrdtmv_druid_1_defstat", "scrdtit_starry_cloak", "scrdtit_volcano_spear", "scrdtit_seashell_shield", "scrdtit_sacred_shield", "scr_encounter_internal_data", "scr_projectile_internal_data", "scrdtmv_hblade_0", "scrdtit_pajama_hat", "scrdtit_meteor_staff", "scrdtit_peridot_rapier", "scrdtit_altair_dagger", "scrdt_quest_dialog", "scrdtit_rusted_greatsword", "scrdtmv_bruiser_0_defstat", "scrdtit_mermaid_scale", "scrdtit_boulder_shield", "sfxset_player_hblade", "scrdtanim_enemy_sanct", "scranim_data_set_external", "scrdtit_spinning_chakram", "scrdtit_flame_bow", "scrdtit_falconfeather_dagger", "sfxset_dark", "scrdt_quest_minstoryclear", "scrdtmv_dancer_3", "scrdtit_stonebreaker_staff", "scrext_trg_replace", "scrdtmv_hblade_3_defstat", "scrdtit_onepiece_swimsuit", "scrdtmv_dancer_2_deftrig", "scrdtmv_defender_3_deftrig", "scrdtmv_wizard_1_deftrig", "scrdtmv_shadow_1_defstat", "scrdtit_unknown_item", "scr_readsheet_pattern_timing", "scr_item_data_set_transformids", "scrdt_quest_shopreq", "scr_mod_refresh_lists", "trigger_patt", "scrdtmv_druid_3", "scrdtit_raindrop_earrings", "screnc_data_set", "scrdtanim_enemy_wolf", "scrdtit_phoenix_charm", "scrdtit_raiju_crown", "screnc_data_set_external", "scrdtqu_credits", "scrdtmv_pyro_3", "scrdtmv_gunner_0", "scrdtit_darkcloud_necklace", "scrdtit_darkmage_charm", "scrdtit_smoke_shield", "scrdtit_teacher_knife", "sfxset_dark2", "scr_hbstatus_internal_data", "scrit_data_set_external", "scrdtit_icicle_earrings", "scrdtit_heavens_codex", "scrdten_training_dummy", "scrdtmv_hammer_3_defstat", "scrdtit_demon_horns", "scrdtmv_hblade_1_deftrig", "scrdtit_firescale_corset", "scrdtit_blue_rose", "scrdtit_poisonfrog_charm", "scrdtit_waterfall_polearm", "scritex_set_type", "scrdtit_cavers_cloak", "scrdtmv_hblade_2_deftrig", "scrdtmv_assassin_2_deftrig", "scrdtit_sleeping_greatbow", "scrdtit_winged_cap", "scrdtit_lion_charm", "scrtr_data_stat", "scrdthbs_order", "scranim_data_attanim", "scrhwext_data_set_local", "scrdtit_comforting_coat", "scrdtit_hexed_blindfold", "scrdtit_fossil_dagger", "scrdtenc_lighthouse", "scrdtenc_aurum", "scrdtit_large_umbrella", "scr_move_internal_data", "scrdtmv_gunner_2_defstat", "scrdtmv_shadow_2_emerald_trig", "scrdtit_black_wakizashi", "scrdtit_royal_staff", "scrdtit_deathcap_tome", "scrdtit_timespace_dagger", "scrdtit_youkai_bracelet", "scrdtpdn_donut", "scrdtit_ribboned_staff", "scrdtit_apple_plate", "scrdtit_book_of_cheats", "sfxdt_data_set", "scrdtit_sweet_taffy", "scrdtmv_assassin_2_defstat", "scrdtmv_hammer_1_deftrig", "scrdtit_thunderclap_gloves", "sfxset_empty", "scranim_data_rotation", "scranim_data_set_external_headoffy", "scritex_set_chargetype", "scrdt_quest_npc_e", "scrdtqu_hearts", "scrdtit_witchs_cloak", "scrdtit_shinobi_tabi", "scrdtit_whiteflame_staff", "scrdtit_desert_earrings", "scrtr_data_trig", "scr_mod_debug", "scrdtqu_beasts", "scrdtqu_spell", "scrdtit_straw_hat", "scrdtmv_dancer_0_defstat", "scrdtit_killing_note", "scrdtit_tactician_rod", "scrdtqu_dragon", "object_get_depth", "scrdtmv_wizard_3", "scrit_data_set", "scrdtit_twinstar_earrings", "sfxset_blade", "scr_readsheet_internal", "scrdtit_vanilla_wafers", "scrdtmv_dancer_0_deftrig", "scrdtit_darkstorm_knife", "scral_data_color", "scrdtpjl_laser", "scr_readsheet_populate_possible_items", "scrdtmv_druid_1", "scrdtmv_spsword_1_deftrig", "scrdtit_spiked_shield", "scrhwext_data_set_notch", "scrdtmv_defender_1_defstat", "scrdtit_firststrike_bracelet", "scrdthbs_stoneskin", "scrdthbs_curse", "scr_readsheet_pattern_new", "scrdtmv_sniper_0", "scrdtmv_wizard_1_defstat", "scrdtmv_sniper_3_deftrig", "scrdtit_vampiric_dagger", "scrdtit_holy_greatsword", "scrdtit_nova_crown", "scr_trinket_internal_data_dlc", "scr_randomizeditem_set_ids", "scrdtmv_druid_0", "trigger_cond", "trigger_lpatt", "scrdtmv_shadow_3_deftrig", "scrdtmv_assassin_1_deftrig", "scrdtit_haunted_gloves", "scrdthbs_haste", "scrdtmv_gunner_1_deftrig", "scrdtit_lancer_gauntlets", "scrdtit_watermage_pendant", "scrdtpjb_fire", "scrdthbs_burn", "scrdtmv_assassin_3", "scrdtmv_none_2", "scrdtit_amethyst_bracelet", "scr_readsheet_items", "scrdtit_staff_of_sorrow", "scrdtmv_ancient_2_defstat", "scrdtit_emerald_chestplate", "scrdtit_occult_dagger", "scritex_trig_arr", "scrit_data_cdinfo", "scrdtmv_hblade_2", "sfxset_player_druid", "scrdten_depths", "scrdtmv_dancer_3_defstat", "scrdtmv_shadow_0", "scrdtmv_pyro_2_defstat", "scrdtmv_shadow_2_defstat", "scrdtit_sawtooth_cleaver", "scrdtmv_sniper_0_deftrig", "scrdtit_blackhole_charm", "scrdtit_frost_dagger", "scrdtit_sand_shovel", "scrdtit_sparrow_feather", "scrdthbs_flutterstep", "scranim_data_set", "scrdtanim_enemy_depths", "scrdtit_frozen_staff", "scrdtit_wind_spear", "scrdtmv_pyro_1_defstat", "scrdtit_butterfly_hairpin", "scrdtqu_fey", "scrdtit_lonesome_pendant", "scrdtmv_hammer_2", "scrdtit_garnet_staff", "scrdtpjb_light2", "sfxset_player_dancer", "scrdtanim_enemy_geode", "scranim_data_set_external_headoffx", "scrdtit_colorful_earrings", "scrdtit_crown_of_swords", "scrdtmv_druid_3_defstat", "scrdtmv_sniper_3_defstat", "scrdtmv_ancient_3_deftrig", "scrdtit_golems_claymore", "scrdtmv_defender_2_defstat", "scrdtit_queens_crown", "scrdtit_marble_clasp", "scrdtit_ruins_sword", "scrdtmv_bruiser_2_deftrig", "scrdtmv_gunner_1", "scr_pickup_internal_data", "scrhbs_data_elm", "scrit_data_elm", "scrdtmv_hammer_0_deftrig", "scrdtmv_pyro_0_deftrig", "scrdtit_brightstorm_spear", "sfxset_player_wizard", "scrdtmv_assassin_1", "scrdtit_timemage_cap", "scrdtit_eaglewing_charm", "scrdtit_bolt_staff", "scrdtit_battlemaiden_armor", "scrdtanim_lq_auto", "scrdtmv_assassin_0_defstat", "scrdtmv_shadow_1", "scrhw_data_elm", "scr_readsheet_populate_trgfunctions", "scritex_read_trigger", "scrdtit_spear_of_remorse", "rgb", "scrdtmv_wizard_2_deftrig", "scrdtmv_bruiser_3", "scrdtit_vega_spear", "scrdtpjb_dark2", "scrdtit_ninjutsu_scroll", "scrdtenc_nest", "scr_readsheet_external", "scrdtit_bluebolt_staff", "sfxset_player_pyro", "scr_readsheet_color", "scrhbsex_set_refreshtype", "scranim_data_headoffy", "scrdtmv_bruiser_3_deftrig", "sfxdt_data_row", "scrit_elmret", "scrdtmv_defender_0", "scrdtit_feathered_overcoat", "screnc_data_pattern", "sfxset_player_bruiser", "subarray_2d_simple", "scrhbsex_set_targettype", "scrdtit_righthand_cast", "scrdtmv_wizard_0_deftrig", "scrdtit_blacksteel_buckler", "scrdtit_darkglass_spear", "scr_ally_internal_data", "scrdthbs_sniper", "scrdtqu_saya", "scrdtit_crown_of_love", "scrdtit_saltwater_staff", "scrdtmv_druid_1_deftrig", "scrdtmv_ancient_0_defstat", "scrdtmv_wizard_0", "scrdtit_sacredstone_charm", "scrhbs_data_set_ext", "scrdtit_rosered_leotard", "scrdtit_hells_codex", "scrdten_dragons", "scrdten_geode", "scrdtit_ghost_spear", "scritex_set_hbinput", "sfxset_light", "scrdtit_sun_sword", "scrdtmv_bruiser_1_defstat", "scrdtit_windbite_dagger", "scrdtit_kappa_shield", "scrdtpk_pickup", "scrdtpjb_water", "scrdtanim_enemy_dragon", "scr_hexchar_to_int", "scrdtit_spark_of_determination", "scrdtit_fanciful_book", "scrdtmv_spsword_1_defstat", "scrdtit_sapphire_violin", "scrdthbs_ghostflame", "scrhw_data_set", "scrdt_quest_npc", "scrdtmv_druid_2_deftrig", "scrdtmv_ancient_1", "scrdtmv_ancient_1_defstat", "scrdtit_granite_greatsword", "scrdtit_kyou_no_omikuji", "scrdtenc_sanct", "scrpj_line_data_row", "scrpj_donut_data_row", "scrdthbs_lucky", "screnc_data_set_replace_local", "scrit_data_hitbox", "scrdtit_sewing_sword", "scrdtit_sharpedged_shield", "scr_enemy_internal_data", "scr_read_dialog_data", "scrdtit_handmade_charm", "scren_data_elm", "scrdtmv_bruiser_2_defstat", "scrdtmv_pyro_3_deftrig", "scrdtit_redwhite_ribbon", "scrdtit_crane_katana", "scral_data_elm", "scritex_set_cdtype", "scrdtmv_dancer_2", "scrdtmv_pyro_1", "scrdtit_rockdragon_mail", "scrdthbs_bleed", "scrdthbs_frogs", "scrdthbs_wolf", "scrdtmv_hblade_0_defstat", "scrdtmv_ancient_0_deftrig", "scrdtit_chrome_shield", "scrdtit_storm_petticoat", "scrdtit_compound_gloves", "scrdtenc_outskirts", "scrpj_laser_data_row", "scrpj_ray_data_row", "scr_hallway_internal_data", "scr_readsheet_pattern", "scrdtit_memory_greatsword", "scrdtit_rain_spear", "scrdtmv_druid_3_deftrig", "scrdtit_snipers_eyeglasses", "scrdtit_sandpriestess_spear", "sfxset_player_defender", "scrdtqu_shopkeeper", "scr_loadsprite_clear", "scrdtit_greatsword_pendant", "scrdtmv_hammer_2_deftrig", "scrdtmv_pyro_0", "scrdtit_reddragon_blade", "scrdtit_sacred_bow", "scrdtit_venom_hood", "scrdtit_jade_staff", "scrdtit_shield_of_smiles", "scrdtit_giant_paintbrush", "scrdtmv_gunner_2_deftrig", "scrdtmv_wizard_0_defstat", "scrdtit_maid_outfit", "scrdtit_performers_shoes", "scrdtmv_druid_2", "scrdtmv_sniper_2", "scrdtmv_assassin_1_defstat", "scrdtit_hawkfeather_fan", "scrdtit_grasswoven_bracelet", "scrdtanim_enemy_aurum", "scrdtmv_hammer_0_defstat", "scrdtit_snakefang_dagger", "scrdtit_red_tanzaku", "scrdtpk_potion", "scrdtenc_keep", "scrdtanim_player", "scrdtmv_none_3", "scrdtmv_defender_1", "scrdtmv_assassin_3_defstat", "scrdtit_silver_coin", "scrdtit_sunflower_crown", "scrdtit_ivy_staff", "scrdtpjb_light", "sfxset_player_sniper", "sfxset_player_ancient_pet", "scr_readsheet_enemy", "scrdten_darkhall", "scrdtmv_shadow_1_deftrig", "scrdtanim_charselect", "scrdtmv_wizard_2_defstat", "scrdtmv_sniper_1_deftrig", "scrdtit_flamewalker_boots", "scral_data_set", "sfxset_ray", "scr_trinket_internal_data", "scren_data_set_external", "scrdtit_whitewing_bracelet", "scrdtmv_dancer_3_deftrig", "scrdtit_lullaby_harp", "screnc_data_pattern_sp", "sfxset_enemy", "scrdthbs_flow", "scrrsptrg", "scr_readsheet_anim", "scrdtqu_pets", "scrdtit_winter_hat", "scrdtmv_unknown_0", "scrdtmv_ancient_3", "scrdtit_glittering_trumpet", "scrpj_circle_data_row", "scrhbs_data_trig", "scrdtit_snow_boots", "scrdtit_tiny_fork", "scrdtmv_defender_1_deftrig", "scrdtpcr_circle", "sfxset_player_ancient", "scr_lang_get", "scrdtmv_defender_2", "scrdtit_nightingale_gown", "scrdthbs_spark", "scrdt_quest", "scrdtit_glacier_spear", "scrdtmv_gunner_3_defstat", "scrdtmv_pyro_1_deftrig", "scrdtit_calling_bell", "scrdthbs_spsword", "scrnpc_data_elm", "scrdtit_lefthand_cast", "scrdtit_hooked_staff", "scrdtmv_sniper_2_defstat", "scrdtit_darkmagic_blade", "scrdtit_shinsoku_katana", "scrdtit_gladiator_helmet", "scrdtit_tornado_staff", "scrdtit_flamedancer_dagger", "scrdtpjb_dark", "scrdtit_hidden_blade", "scrdten_mice", "scrdtmv_defender_2_deftrig", "scrdtmv_defender_3_defstat", "scrdtit_pyrite_earrings", "scrdtmv_spsword_0_defstat", "scrdtit_ruby_circlet", "scrdtenc_test", "scrdtpn_none", "scrdtit_haste_boots", "scrdthbs_freeze", "scrdtqu_birds", "scrdtmv_bruiser_3_defstat", "scrdtit_robe_of_dark", "scrdten_other", "scrdtmv_hblade_0_deftrig", "scrdtit_leech_staff", "scrdtit_golden_chime", "scrdtit_obsidian_rod", "scrdtit_reflection_shield", "scrpj_data_set", "scrdtanim_enemy_other", "scrdtit_springloaded_scythe", "scrdtmv_bruiser_0", "scrdtmv_wizard_3_defstat", "scrdtit_ninja_robe", "scrdtit_cloud_guard", "sfxset_player_gunner", "scrdtit_bladed_cloak", "scrdtmv_spsword_0", "scrdtmv_spsword_0_deftrig", "scrdtmv_spsword_1", "scrdtit_tiny_hourglass", "scranim_data_elm", "scrpj_cone_data_row", "scrdtqu_wolf", "scrdtit_strongmans_bar", "scrdtmv_sniper_2_deftrig", "scrdtmv_ancient_0", "scrdtmv_wizard_3_deftrig", "scrdtit_mimick_rabbitfoot", "scr_read_quest_data", "scrdtit_nightstar_grimoire", "scrdtit_chemists_coat", "sfxset_light2", "scrdtmv_spsword_2_defstat", "scrdtmv_defender_0_deftrig", "scrdtmv_bruiser_1", "scrdtenc_geode", "sfxset_water", "scrdthbs_paint", "scrdtanim_enemy_reflection", "scr_readsheet_pattern_instruction", "scrdt_quest_minshopvisit", "scrdt_quest_deco_flag", "scrnpc_data_set", "trigger_target", "scrdtit_jesters_hat", "scrdtmv_hammer_2_defstat", "scrdtit_necronomicon", "sfxset_player_shadow", "sfxset_item_hydra", "scrdtqu_frog", "scrdtmv_hammer_3", "csv_load", "scr_readsheet_hallway", "scrdtit_coldsteel_shield", "scrmv_elm_colors", "scrdtmv_druid_0_deftrig", "scrdtmv_none_0", "scrdtit_bloodhound_greatsword", "scrdthbs_none", "scr_npc_internal_data", "scrdtmv_dancer_2_defstat", "scrdtmv_sniper_0_defstat", "scrdtmv_druid_0_defstat", "scrdtit_opal_necklace", "scrdtit_usagi_kamen", "scrdtit_robe_of_light", "scrdtit_rainbow_cape", "scrdtit_drill_shield", "scrdtit_staticshock_earrings", "screnc_data_pattern_mp", "scrdthbs_super", "screnumkey", "scr_item_internal_data", "scrdtit_unsacred_pendant", "scrdtit_caramel_tea", "scrdten_pinnacle", "scrdtmv_dancer_1_defstat", "scrdtmv_assassin_0", "scrdtit_blackwing_staff", "scrdtit_reaper_cloak", "scrdtit_greysteel_shield", "scrdtenc_test2", "scrdtmv_hblade_2_defstat", "scrdtmv_gunner_3", "scrdtmv_spsword_2_deftrig", "scrdtit_purification_rod", "scr_trinket_internal_data_ring", "scrdtenc_darkhall", "sfxset_player_spsword", "scrdtanim_enemy_frog", "scrdtqu_trueend", "scrdtmv_assassin_2", "scrdtpjb_blade", "scrdtpjr_ray", "scrdtmv_spsword_3_defstat", "scrdtmv_shadow_3_defstat", "scrdtit_bloodflower_brooch", "scr_readsheet_anim_simple", "scr_readsheet_encounter", "scrdt_quest_extraimage", "scrdtit_beach_sandals", "scrdten_sanct", "scrdtmv_wizard_1", "scrdten_wolves", "scrdtmv_assassin_3_deftrig", "scrdtit_divine_mirror", "scr_item_internal_data_dlc", "scrdtmv_spsword_3", "scrdtit_throwing_dagger", "scrdtmv_hammer_1_defstat", "scrdtmv_ancient_3_defstat", "scrdtit_grandmaster_spear", "scrdtmv_pyro_3_defstat", "scrdtanim_enemy_spawn", "scrdtit_miners_headlamp", "scrdtmv_dancer_1_deftrig", "scrdtmv_ancient_1_deftrig", "scrdtit_iron_grieves", "scrdtit_stoneplate_armor", "scrdtenc_lakeside", "scrdtit_sketchbook", "scrdtit_canary_charm", "scrdtit_obsidian_hairpin", "sfxdt_pitch", "scrdt_quest_nonstep", "scrit_data_stat", "scrdtmv_gunner_2", "scrdtit_crown_of_storms", "sfxset_player_assassin", "scrdtmv_sniper_1", "scrdtit_shrinemaidens_kosode", "scrhbsex_read_trigger", "scrdtqu_ghost", "scrdtmv_ancient_2_deftrig", "scrdtmv_bruiser_1_deftrig", "scrdtit_ballroom_gown", "scrdtit_oni_staff", "scrhbs_data_set", "scrdtmv_sniper_3", "scrdtit_thiefs_coat", "scrdtit_lightning_bow", "scr_animations_internal_data", "trigger_set", "scrdtmv_pyro_0_defstat", "scrdtpk_upgrade", "scrdtmv_bruiser_2", "scrdtit_crowfeather_hairpin", "scrdtit_shadow_bracelet", "scrdtit_shockwave_tome", "scrdthbs_strike", "scrdten_birds", "scrdtmv_wizard_2", "scrdtmv_shadow_0_deftrig", "scral_data_stat", "scrdtanim_enemy_bird", "scrdtqu_tassha", "scrdtmv_bruiser_0_deftrig", "scrdtit_ornamental_bell", "scrdtit_butterfly_ocarina", "scrdtit_cursed_candlestaff", "scritex_set_weaptype", "scrdtit_timewarp_wand", "scrdtit_tiny_wings", "scrdtpjb_fire2", "scrdthbs_birds", "scrdtqu_other", "scrdtqu_sanct", "scrdtit_artist_smock", "scrdten_aurum", "scrdtmv_hblade_1_defstat", "scrdtmv_gunner_0_deftrig", "scrdtit_crescentmoon_dagger", "scrdtit_blood_vial", "scrdt_quest_step", "scrit_data_trig", "scrdtit_painters_beret", "scrdtit_moss_shield", "scrdthbs_poison", "scritex_set_hbdisptype", "scrdtmv_hammer_1", "scrdtit_giant_stone_club", "scrdthbs_smite", "scrdtit_darkcrystal_rose", "scrdtmv_defender_0_defstat", "scrdtit_talon_charm", "scrdtenc_toybox", "scrext_trig_line", "scrdtmv_hblade_3", "scrit_elmsw", "scrdtit_diamond_shield", "scrdtit_abyss_artifact", "scr_readsheet_populate_possible_encounters", "scrdtmv_dancer_1", "scrdtmv_defender_3", "scrdtmv_shadow_3", "scr_bookofcheats_triggers", "scrtr_data_elm", "sfxset_fire", "scrhbsex_set_floatingtexttype", "scren_data_set", "scrdtmv_gunner_0_defstat", "scrdtenc_arsenal", "scr_readsheet_populate_enumkeys", "scrdtmv_dancer_0", "scrdtit_curse_talon", "scrdtit_stuffed_rabbit", "scrdtit_clockwork_tome", "scrdtit_golden_katana", "scrdtit_spiderbite_bow", "scrdtit_lost_pendant", "sfxset_dlc", "scrdthbs_counter", "scranim_data_set_external_color", "scrdtit_dark_wings", "scrdtit_night_sword", "scrdtmv_gunner_1_defstat", "scrdtmv_hblade_1", "scrdtmv_shadow_0_defstat", "scr_get_list_from_string", "scrhbs_data_set_external", "scrdtit_pidgeon_bow", "scr_get_color_from_hexstring", "scrdtqu_cat", "scrdtmv_shadow_2_deftrig", "scrdtit_trick_shield", "scrdtmv_spsword_2", "scrdtmv_shadow_2", "scrdtit_moon_pendant", "screnc_data_elm", "scr_readsheet_hbs", "scrhbsex_trig_arr", "scrdtit_pointed_ring", "scrdtmv_assassin_0_deftrig", "scrdtit_eternity_flute", "scrdtit_kunoichi_hood", "scrdtit_pocketwatch", "scrdtit_battery_shield", "sfxset_player_hammer", "scrdtqu_battle", "scrdtit_angels_halo", "scrdtit_bloody_bandage", "scrdtanim_enemy_mouse", "trigger_qpatt", "scrdtmv_ancient_2", "scrdtmv_hammer_0", "scrdtmv_pyro_2", "scrdtit_assassins_knife", "scrdtit_ravens_dagger", "scrdtit_old_bonnet", "scrdtit_mountain_staff", "sfxset_generic", "scrdt_quest_clearcount", "scrdtmv_pyro_2_deftrig", "scrdtit_palette_shield", "scrdtit_topaz_charm", "scrdtit_fairy_spear", "scrdtenc_depths", "scrdthbs_decay", "scrdtanim_npc", "scrdtit_large_anchor", "scrdten_frogs", "scrdtmv_none_1", "scrdtit_gemini_necklace", "scrtr_data_set", "scrdtpjb_invis", "scrdtqu_mouse", "scr_readsheet_sheetlist", "scrdtit_redblack_ribbon", "scrdtit_tough_gauntlet", "scrdtit_tidal_greatsword", "scrdthbs_mice", "scrdthbs_fieldlimit", "scrdtit_iron_pickaxe", "scrdtit_strawberry_cake", "scrdtmv_hblade_3_deftrig", "scrdtit_dragonhead_spear", "scritex_set_treasuretype", "instance_create", "scrdtit_blackbolt_ribbon", "scrdtpcn_cone", "scrdthbs_dlc_enemy", "scrdtit_stirring_spoon", "scrdtit_floral_bow", "scrdtit_quartz_shield", "scrdtit_lapis_sword", "scrdtit_aquamarine_bracelet", "sfxset_fire2", "scrdthbs_player",
        // "lsm_sprites_players", "lsm_sprites_next_encounter", "scr_hallwaygen_outskirts_n", "scr_stage_get_name", "scr_hallwaygen_credits_dlcstart", "scr_hallwayprogress_choose_halls", "lsm_delete_other_sprites", "scr_lang_string", "scr_hallwaygen_test_trailer", "scr_stagefirst_mapdeco_sync", "scr_hallwaygen_tutorial", "scr_stage_get_subimg", "scr_stagefirst_get_name", "scr_questcontroller_make_dialog_list", "scr_music_transfer", "lsm_add_entry", "scr_hallwaygen_darkhall", "lsm_sprites_charselect", "scr_timegt_reset", "lsm_sprites_battle", "scr_loadsprite_get", "scr_stagefirst_toybox", "math_fit_within", "scr_hallwayprogress_generate_key", "scr_hallwaygen_nest", "scr_lang_get", "lsm_refresh_mod_sprites", "scr_stagefirst_change", "math_coinflip", "scr_difficulty", "scr_diffswitch", "scr_hallwaygen_outskirts", "lsm_add_entry_by_anim_key", "scr_obswitch", "scr_hallwayprogress_choose_halls_credits", "scr_loadsprite_is_loaded", "scr_hallwaygen_reflection", "scr_hallwayprogress_choose_halls_credits_dlc", "scr_hallwayprogress_choose_halls_toybox", "scr_hallwayprogress_generate", "scr_hallwaygen_lakeside", "scr_hallwaygen_geode", "scr_hallwaygen_chaos_hallway", "scr_hallwayprogress_change_stage", "scr_stagefirst_available", "scr_hallwaygen_lighthouse", "scr_stagefirst_get_mappage_index", "scr_hallwayprogress_shuffle_items", "scr_hallwaygen_streets", "scr_hallwaygen_chaos_mid", "scr_hallwaygen_mod", "math_random_switch", "scr_hallwaygen_test_finalboss", "lsm_sprites_default", "scr_hallwayprogress_choose_halls_fixedpath", "lsm_sprites_quest", "scr_stage_music_firstnotch_transfer", "scr_hallwaygen_credits_dlcsecondlast", "scr_stagefirst_diffdisp_sync", "scr_stagefirst_get_color", "scr_hallwayprogress_add_alltreasure", "scr_hallwaygen_toybox", "scr_hallwayprogress_shuffle_encounters", "lsm_sprites_mods", "scr_hallwayprogress_add_unlocks", "scr_hallwaygen_pinnacle", "lsm_sprites_store", "scr_hallwaygen_credits", "scr_hallwayprogress_setpos", "scr_hallwaygen_credits_dlcend", "scr_hallwaygen_keep", "scr_hallwaygen_credits_outskirts", "scr_hallwayprogress_pop_encounter", "scr_hallwaygen_depths", "scr_hallwaygen_aurum", "scr_hallwaygen_arsenal", "scr_hallwaygen_chaos_intro", "scr_stagefirst_hw_has_priority", "scr_hallwaygen_sanct", "scr_stage_change", "scr_hallwaygen_test", "scr_hallwaygen_credits_pinnacle", "scr_hallwayprogress_subimgs",
    ];
    // Calltree stuff
    // private readonly List<string> dumpScripts = [
    //     // Seed generation scripts
    //     // "scr_music_transfer", "scr_hallwayprogress_generate", "scr_lang_string", "scr_hallwaygen_nest", "scr_hallwayprogress_shuffle_items", "scr_hallwaygen_outskirts", "scr_difficulty", "scr_diffswitch", "scr_stagefirst_diffdisp_sync", "scr_stagefirst_get_color", "math_random_switch", "scr_hallwayprogress_choose_halls_credits", "scr_hallwaygen_reflection", "scr_hallwayprogress_choose_halls_credits_dlc", "scr_hallwayprogress_choose_halls_toybox", "scr_stage_get_name", "scr_hallwayprogress_change_stage", "scr_hallwaygen_chaos_hallway", "scr_hallwaygen_geode", "scr_hallwaygen_lakeside", "scr_hallwaygen_lighthouse", "scr_stage_get_subimg", "scr_stage_change", "scr_hallwaygen_chaos_mid", "scr_hallwaygen_mod", "scr_hallwaygen_streets", "scr_hallwaygen_test_finalboss", "scr_hallwayprogress_shuffle_encounters", "scr_questcontroller_make_dialog_list", "scr_hallwayprogress_choose_halls_fixedpath", "scr_stagefirst_hw_has_priority", "scr_hallwaygen_credits_dlcsecondlast", "scr_hallwayprogress_add_alltreasure", "scr_hallwayprogress_setpos", "scr_hallwayprogress_pop_encounter", "scr_hallwaygen_toybox", "scr_hallwayprogress_add_unlocks", "scr_hallwaygen_pinnacle", "scr_stagefirst_get_name", "scr_stagefirst_mapdeco_sync", "scr_hallwaygen_credits", "scr_hallwayprogress_subimgs", "scr_hallwaygen_credits_dlcend", "scr_stagefirst_toybox", "scr_hallwaygen_credits_outskirts", "scr_hallwaygen_keep", "scr_hallwaygen_aurum", "scr_hallwaygen_depths", "scr_hallwaygen_arsenal", "scr_hallwaygen_chaos_intro", "scr_stagefirst_change", "scr_hallwaygen_sanct", "math_fit_within", "scr_hallwaygen_test", "scr_hallwaygen_credits_pinnacle", "scr_stage_music_firstnotch_transfer", "scr_hallwaygen_credits_dlcstart", "scr_hallwaygen_outskirts_n", "scr_hallwayprogress_choose_halls", "math_coinflip", "scr_hallwayprogress_generate_key", "scr_hallwaygen_test_trailer", "scr_stagefirst_available", "scr_hallwaygen_tutorial", "scr_hallwaygen_darkhall", "scr_difficulty_change",
    //     // Some init scripts
    //     // "scr_init_adventure_map", "scr_init_adventure_const", "scr_triggerglobals_reset", "scr_randomizeditem_init", "scr_lobbyhost_buff_adventuredata",
    //     // Scrc init (contains seed gen scripts)
    //     // "scr_loadsprite_get", "scr_music_transfer", "scr_hallwaygen_nest", "scr_chat_edit_name", "scr_lang_string", "scr_hallwayprogress_generate", "scr_statcontroller_refresh", "scr_obswitch", "scr_stringsanitize_remove_word", "scr_lang_font_load", "lsm_add_entry_by_anim_key", "scr_hallwayprogress_shuffle_items", "scr_hallwaygen_outskirts", "scr_get_gy", "scr_difficulty", "scr_diffswitch", "lsm_sprites_default", "scr_stagefirst_diffdisp_sync", "scr_stagefirst_get_color", "scr_stagefirst_get_message", "math_random_switch", "scr_hallwayprogress_choose_halls_credits", "scr_hallwaygen_reflection", "scr_loadsprite_is_loaded", "scr_hallwayprogress_choose_halls_credits_dlc", "scr_hallwayprogress_choose_halls_toybox", "scr_stage_get_name", "lsm_sprites_quest", "math_bound", "scr_lang_fw_switch_ext", "scr_lang_get", "scr_hallwaygen_chaos_hallway", "scr_hallwaygen_geode", "scr_hallwaygen_lakeside", "scr_hallwayprogress_change_stage", "scr_client_plbin_check", "scr_hallwaygen_lighthouse", "scr_stage_get_subimg", "scr_chat_add_message", "scr_lang_get_scale_ext", "scr_string_convert", "scr_hallwaygen_chaos_mid", "scr_hallwaygen_streets", "scr_hallwaygen_mod", "lsm_sprites_mods", "scr_stage_change", "scr_lang_get_scale_dialog_ext", "scr_hallwaygen_test_finalboss", "object_get_depth", "scr_hallwayprogress_shuffle_encounters", "lsm_sprites_store", "scr_hallwayprogress_choose_halls_fixedpath", "scr_questcontroller_make_dialog_list", "scr_play_sound", "scr_sound_get_default_volume", "scr_stagefirst_hw_has_priority", "scr_stringsanitize_computer", "scr_hallwaygen_credits_dlcsecondlast", "scr_hallwayprogress_setpos", "scr_hallwayprogress_add_alltreasure", "scr_hallwayprogress_pop_encounter", "scr_hallwaygen_toybox", "scr_lang_get_char_width_ext", "scr_stringsanitize_lowercase_letters", "scr_hallwayprogress_add_unlocks", "scr_chat_add_mesage_system", "scr_hallwaygen_pinnacle", "scr_stagefirst_get_name", "scr_stagefirst_mapdeco_sync", "scr_hallwaygen_credits", "lsm_sprites_next_encounter", "lsm_sprites_players", "scr_hallwaygen_credits_dlcend", "scr_hallwayprogress_subimgs", "scr_stagefirst_toybox", "scr_hallwaygen_credits_outskirts", "scr_hallwaygen_keep", "scr_stringsanitize_is_char", "scr_hallwaygen_aurum", "scr_hallwaygen_depths", "scr_lang_set_font_ext", "scr_hallwaygen_arsenal", "scr_hallwaygen_chaos_intro", "lsm_sprites_charselect", "scr_timegt_reset", "math_fit_within", "scr_input_check_nonlocal", "scr_string_sanitize", "scr_hallwaygen_sanct", "scr_stagefirst_change", "lsm_sprites_battle", "scr_hallwaygen_test", "scr_hallwaygen_credits_pinnacle", "scr_get_gx", "lsm_delete_other_sprites", "scr_stage_music_firstnotch_transfer", "scr_lang_get_font_ext", "scr_hallwaygen_credits_dlcstart", "scr_hallwaygen_outskirts_n", "lsm_refresh_mod_sprites", "scrc_h_adventure_set_data", "instance_create", "scr_stringsanitize_human", "scr_difficulty_change", "scr_hallwayprogress_choose_halls", "math_coinflip", "scr_hallwaygen_test_trailer", "scr_hallwayprogress_generate_key", "lsm_add_entry", "scr_stagefirst_available", "scr_hallwaygen_tutorial", "scr_stagefirst_get_mappage_index", "scr_chat_edit_text", "scr_hallwaygen_darkhall",
    //     // Online patterns variables
    //     // "patvar_add_range", "patvar_add_precision", "scr_init_online_patterns_var", "patvar_add", "patvar_add_range_int",
    //     // chat stuff
    //     // "scrc_lb_postmessage", "scrc_lb_newmessage", "scr_lobbyhost_call_all", "scrom_read_num", "scrom_read_string", "scrc_lb_newchataction",
    //     "rgb",
    //     // dialog stuff
    //     "dialog_end", "scr_timegt_reset", "scr_dialog_update_menu", "math_bound", "scr_lang_get", "scr_lang_fw_switch_ext", "scr_lang_set_font_ext", "scr_input_check", "dialog_change_textscale", "dialog_animation", "dialog_npc_face_center", "math_fit_within", "scr_lang_get_font", "scr_deco_store_set_expression_bird", "scr_dialog_handle_flag_color", "scr_change_highlight_by_color", "dialog_change_speaking_none", "dialog_npc_face_direction", "scr_charanim_reset_stack", "scr_char_change_animation_base", "scr_client_plbin_check", "scr_dialog_update", "scr_timect_reset", "scr_dialog_update_action", "scr_timect_check", "scr_lang_get_scale_ext", "scr_lang_get_char_width_ext", "scr_string_convert", "scr_deco_store_set_expression_cat", "scr_dialog_add_message", "scr_lang_get_scale_dialog_ext", "scr_deco_store_set_expression_wolf", "scr_stringsprite_load", "dialog_sound", "scr_obswitch", "scr_udpcont_send_now", "scr_get_gx", "math_lerp_between", "dialog_change_speaking_npc", "scr_lang_set_font", "scr_lang_fe_switch", "scr_lang_font_load", "scr_lang_get_font_ext", "scr_deco_store_set_talk", "scr_lang_fw_switch", "scr_deco_store_set_expression", "scr_dialog_check_skip", "scr_play_sound_pitch", "scr_play_sound2", "scr_char_set_forcedmarch", "scr_notchex_change_state", "scr_dialog_update_line", "scr_dialog_handle_flags", "dialog_change_highlight_calc_color", "scr_get_gy", "math_percent_between", "scr_dialog_next_line", "scr_sound_get_default_volume", "dialog_change_speaking_mystery", "dialog_mini_move_relative", "dialog_mini_move_absolute", "scr_play_sound", "scr_timect_update", "math_diff", "scr_dialog_read_instructions", "scr_chat_edit_text", "scr_char_play_animation_attack", "scr_char_play_animation_ext", "dialog_change_speaking_enemy", "dialog_npc_face_player", "scr_dialog_check_input", "scr_timegt_update", "math_lerp_smooth_gt",
    // ];
    private List<String> dumpFuncs = new List<string> {"random_set_seed","ds_list_copy","ds_list_shuffle","ds_list_create","ds_list_size","ds_list_find_value","ds_list_delete","ds_list_add","array_shuffle","array_shuffle_ext","random","random_range","irandom","irandom_range","choose","randomize", "ds_map_clear", "ds_map_create", "merge_color", "ds_map_destroy", "array_get", "is_undefined", "ds_map_find_value", "ds_map_set", "ini_read_string"};
    // Only log these scripts if used inside other (logged) scripts
    private List<String> badScripts = new List<string> {"scr_difficulty", "scr_stagefirst_toybox", "math_fit_within", "scr_lang_string", "scr_stagefist_available", "scr_diffswitch", "scr_get_gx", "scr_get_gy", "scr_lang_get", "math_bound", "scr_lang_set_font_ext", "scr_lang_get_font_ext", "scr_obswitch", "scr_loadsprite_get", "scr_loadsprite_is_loaded", "scr_lang_fw_switch_ext", "scr_input_check_nonlocal", "scr_stagefirst_available", "scr_client_plbin_check", "scr_statcontroller_refresh", "scr_lang_get_scale_ext", "scr_triggerglobals_reset", "scr_timegt_reset", "scr_timegt_update", "scr_lang_fw_switch", "math_lerp_between", "scr_lang_fe_switch", "math_percent_between", "math_diff", "scr_timect_reset", "scr_timect_update", "math_lerp_smooth_gt", "scr_lang_set_font", "scr_timect_check", "scr_lang_get_font", "scr_char_change_animation_base", "scr_input_check"};
    private string dumpFilename = "dump/definitions.txt";
    private bool isInsideScr = false;
    private bool shouldDumpFuncs = true;


    public void StartEx(IModLoaderV1 loader, IModConfigV1 modConfig) {
        this.rnsReloadedRef = loader.GetController<IRNSReloaded>();
        this.hooksRef = loader.GetController<IReloadedHooks>()!;
        this.scannerRef = loader.GetController<IStartupScanner>();
        this.logger = loader.GetLogger();

        if (this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            rnsReloaded.OnReady += this.Ready;
        }

        this.configurator = new Configurator(((IModLoader) loader).GetModConfigDirectory(modConfig.ModId));
        this.config = this.configurator.GetConfiguration<Config.Config>(0);
        this.config.ConfigurationUpdated += this.ConfigurationUpdated;

        if (this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)) {
            this.Scan("E8 ?? ?? ?? ?? 44 89 6D ?? F2 0F 11 45 ?? 41 B1", addr => {
                this.randomHook = hooks.CreateHook<RandomDelegate>(this.RandomDetour, addr);
                this.randomHook.Enable();
                this.randomHook.Activate(); });
            this.Scan("E8 ?? ?? ?? ?? 0F 28 F0 8B 4C 24 ?? 83 E1 ?? 8B C7", addr => {
                this.randomRangeHook = hooks.CreateHook<RandomRangeDelegate>(this.RandomRangeDetour, addr);
                this.randomRangeHook.Enable();
                this.randomRangeHook.Activate(); });
            this.Scan("E8 ?? ?? ?? ?? 0F 28 F8 41 8B 4F ?? 83 E1 ?? 8B C6", addr => {
                this.irandomHook = hooks.CreateHook<IRandomDelegate>(this.IRandomDetour, addr);
                this.irandomHook.Enable();
                this.irandomHook.Activate(); });
            this.Scan("E8 ?? ?? ?? ?? 0F 28 F0 41 8B 4D ?? 83 E1 ?? 8B C6", addr => {
                this.irandomRangeHook = hooks.CreateHook<IRandomRangeDelegate>(this.IRandomRangeDetour, addr);
                this.irandomRangeHook.Enable();
                this.irandomRangeHook.Activate(); });
        }

    }
    private void ConfigurationUpdated(IUpdatableConfigurable newConfig) {
        this.config = (Config.Config) newConfig;
    }
    public void Ready() {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
            && this.scannerRef != null
        ) {
            // rnsReloaded.LimitOnlinePlay();

            File.Delete(this.dumpFilename);
            this.InitHooks();
        }
    }

    public void Scan(string sig, Action<nint> callback) {
        if (this.scannerRef!.TryGetTarget(out var scanner)) {
            scanner.AddMainModuleScan(sig, status => {
                if (status.Found) {
                    var BaseAddr = Process.GetCurrentProcess().MainModule!.BaseAddress;
                    var addr = BaseAddr + status.Offset;
                    if (sig.StartsWith("E8") || sig.StartsWith("E9")) {
                        addr += 5 + Marshal.ReadInt32(addr + 1);
                    }
                    callback(addr);
                } else {
                    this.logger.PrintMessage($"Failed to scan {sig}", Color.Red);
                }
            });
        }
    }

    public void InitHooks() {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
        ) {
            var gameSpeedScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scrbp_gamespeed") - 100000);
            this.gameSpeedHook = hooks.CreateHook<ScriptDelegate>(this.gameSpeedDetour, gameSpeedScript->Functions->Function)!;
            // this.gameSpeedHook.Activate();

            var encounterStartScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scrdt_encounter") - 100000);
            this.encounterStartHook = hooks.CreateHook<ScriptDelegate>(this.encounterStartDetour, encounterStartScript->Functions->Function)!;
            this.encounterStartHook.Activate();

            var chooseHallsScript = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("scr_hallwayprogress_choose_halls") - 100000);
            // this.chooseHallsHook = hooks.CreateHook<ScriptDelegate>(this.chooseHallsDetour, chooseHallsScript->Functions->Function)!;
            // this.chooseHallsHook.Activate();

            var script = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("ipat_hblade_0") - 100000);
            this.hookMap["ipat_hblade_0"] = hooks.CreateHook<ScriptDelegate>(this.hbladePrim, script->Functions->Function)!;
            this.hookMap["ipat_hblade_0"].Activate();
            this.hookMap["ipat_hblade_0"].Disable();

            foreach (var funcStr in this.dumpScripts) {
                this.CreateScriptHook(funcStr);
            }
            foreach (var funcStr in this.dumpFuncs) {
                this.CreateFuncHook(funcStr);
            }

            // var arrayShuffleAddr = (nint)rnsReloaded.GetTheFunction(rnsReloaded.CodeFunctionFind("array_shuffle")!.Value).Routine;
            // this.arrayShuffleHook = hooks.CreateHook<RoutineDelegate>(this.ArrayShuffleDetour, arrayShuffleAddr);
            // this.arrayShuffleHook.Activate();

            // var arrayShuffleExtAddr = (nint)rnsReloaded.GetTheFunction(rnsReloaded.CodeFunctionFind("array_shuffle_ext")!.Value).Routine;
            // this.arrayShuffleExtHook = hooks.CreateHook<RoutineDelegate>(this.ArrayShuffleExtDetour, arrayShuffleExtAddr);
            // this.arrayShuffleExtHook.Activate();

            // var dsListShuffleAddr = (nint)rnsReloaded.GetTheFunction(rnsReloaded.CodeFunctionFind("ds_list_shuffle")!.Value).Routine;
            // this.dsListShuffleHook = hooks.CreateHook<RoutineDelegate>(this.DSListShuffleDetour, dsListShuffleAddr);
            // this.dsListShuffleHook.Activate();

            // var dsListCopyAddr = (nint)rnsReloaded.GetTheFunction(rnsReloaded.CodeFunctionFind("ds_list_copy")!.Value).Routine;
            // this.dsListCopyHook = hooks.CreateHook<RoutineDelegate>(this.DSListCopyDetour, dsListCopyAddr);
            // this.dsListCopyHook.Activate();

            var randomSetSeedAddr = (nint)rnsReloaded.GetTheFunction(rnsReloaded.CodeFunctionFind("random_set_seed")!.Value).Routine;
            this.randomSetSeedHook = hooks.CreateHook<RoutineDelegate>(this.RandomSetSeedDetour, randomSetSeedAddr);
            // this.randomSetSeedHook.Activate();

            // var chooseAddr = (nint)rnsReloaded.GetTheFunction(rnsReloaded.CodeFunctionFind("choose")!.Value).Routine;
            // this.chooseHook = hooks.CreateHook<RoutineDelegate>(this.ChooseDetour, chooseAddr);
            // this.chooseHook.Activate();

            // this.itemData = rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "itemData");
            // this.enemyData = rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "enemyData");
        }
    }

    // Thanks Bouny
    private Dictionary<string, string> Enumerate(
        IRNSReloaded rnsReloaded,
        RValue* value,
        bool ignoreFunctions = false
    ) {
        var result = new Dictionary<string, string>();
        foreach (var key in rnsReloaded.GetStructKeys(value)) {
            var val = rnsReloaded.GetString(rnsReloaded.FindValue(value->Object, key));
            if (val == "function" && ignoreFunctions) continue;
            result[key] = val;
        }
        return result;
    }

    private void findRunMenuStuff() {
        Console.WriteLine("Starting RunMenu stuff");
        if (!this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            return;
        }
        Console.WriteLine("Looping through shit");
        var layers = rnsReloaded!.GetCurrentRoom()->Layers;
        for (CLayer* layer = layers.First; layer != null; layer = layer->Next) {
            var elements = layer->Elements;
            CLayerElementBase* element = elements.First;
            string name = (layer->Name == null) ? "Unnamed" : Marshal.PtrToStringAnsi((nint)layer->Name)!;
            while (name == "RunMenu_Blocker" && element != null) {
                if (element->Type == LayerElementType.Instance)
                {
                    CLayerInstanceElement* instance = (CLayerInstanceElement*)element;
                    RValue instanceValue = new RValue(instance->Instance);
                    RValue* value = &instanceValue;
                    foreach (string key in rnsReloaded.GetStructKeys(value))
                    {
                        RValue* val = rnsReloaded.FindValue(value->Object, key);
                        if (key == "hallseed" || key == "notches" || key == "hallsubimg" || key == "hallwayNumber" || key == "hallwayPos" || key == "notchNumber" || key == "stageKey" || key == "stageName" || key == "stageNameKey") {
                            Console.WriteLine($"key={key}, val={val->ToString()}");
                        }
                        // if (key == "hallseed")
                        // {
                        //     hallseed = val;
                        // }
                        // if (key == "notches")
                        // {
                        //     notches = val;
                        // }
                    }
                }
                element = element->Next;
            }
        }
    }

    private void dumpRooms(string filename) {
        Console.WriteLine("Dumping rooms");
        if (!this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            return;
        }
        var data = new Dictionary<String, Dictionary<String, Dictionary<String, String>>>();
        var layers = rnsReloaded!.GetCurrentRoom()->Layers;
        for (CLayer* layer = layers.First; layer != null; layer = layer->Next) {
            var elements = layer->Elements;
            CLayerElementBase* element = elements.First;
            string layerName = (layer->Name == null) ? "Unnamed" : Marshal.PtrToStringAnsi((nint)layer->Name)!;
            data[$"{layerName} ({layer->ID})"] = [];
            while (element != null) {
                string elementName = (element->Name == null) ? "Unnamed" : Marshal.PtrToStringAnsi((nint)element->Name)!;
                if (element->Type == LayerElementType.Instance)
                {
                    CLayerInstanceElement* instance = (CLayerInstanceElement*)element;
                    RValue instanceValue = new RValue(instance->Instance);
                    RValue* value = &instanceValue;
                    data[$"{layerName} ({layer->ID})"][$"{elementName} ({element->ID})"] = this.Enumerate(rnsReloaded, value);
                }
                element = element->Next;
            }
        }
        // var data = new Dictionary<String, Dictionary<String, String>>();
        // var layers = rnsReloaded.ExecuteCodeFunction("layer_get_all", null, null, [])!.Value;
        // var len = rnsReloaded.ExecuteCodeFunction("array_length", null, null, [layers])!.Value;
        // Console.WriteLine(len);
        // Console.WriteLine(len.Type);
        // var len = rnsReloaded.utils.RValueToLong(rnsReloaded.ArrayGetLength(&layers)!.Value);
        // Console.WriteLine(len);
        // for (var i = 0; i < len; i++) {
        //     File.AppendAllLines("dump/array_test.txt", [i.ToString()]);
        //     (&layers)[i].ToString();
        // }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(data, options);
        File.WriteAllText(filename, jsonString);

        // Dump globals
        RValue globalValue = new RValue(rnsReloaded.GetGlobalInstance());
        this.globals = this.Enumerate(rnsReloaded, &globalValue, true);
        jsonString = JsonSerializer.Serialize(this.globals, options);
        File.WriteAllText("dump/globals.json", jsonString);
    }

    private void CreateScriptHook(String scriptName) {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
        ) {
            var script = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId(scriptName) - 100000);

            if (script == null) {
                this.logger.PrintMessage($"Failed to find script {scriptName}!", this.logger.ColorRed);
                return;
            }

            RValue* Detour(
                CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
            ) {
                var hook = this.hookMap[scriptName];
                var isTopScr = false;
                if (!this.isInsideScr) {
                    if (this.badScripts.Contains(scriptName)) {
                        return hook.OriginalFunction(self, other, returnValue, argc, argv);
                    }
                    isTopScr = true;
                    this.isInsideScr = true;
                }

                // if (scriptName == "scr_hallwayprogress_choose_halls") {
                //     this.hookMap["scr_hallwayprogress_choose_halls"].Disable();
                //     for (int i = 0; i < 16; i++) {
                //         Console.WriteLine(rnsReloaded.ExecuteScript("scr_stagefirst_get_name", null, null, [new RValue(i)])!.Value.ToString());
                //     }
                //     this.hookMap["scr_hallwayprogress_choose_halls"].Enable();
                // }

                // Write call
                var call = this.PrintHook(scriptName, argc, argv);
                using (var writer = File.AppendText(this.dumpFilename)) {
                    var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
                    writer.WriteLine(pre + call);
                }

                this.depth += 1;
                if (scriptName == "scr_hallwayprogress_choose_halls") {
                    var seed = rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "mapSeed");
                    using (var writer = File.AppendText(this.dumpFilename)) {
                        var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
                        writer.WriteLine(pre + $"mapseed={seed->ToString()}");
                    }
                }

                returnValue = hook.OriginalFunction(self, other, returnValue, argc, argv);

                // Write return value
                var retString = rnsReloaded.GetString(returnValue);
                if (scriptName == "rgb") {
                    retString = Rgb64ToHex((long)returnValue->Real);
                }
                if (retString != "undefined") {
                    var retVal = $"-> {retString}";
                    using (var writer = File.AppendText(this.dumpFilename)) {
                        var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
                        writer.WriteLine(pre + retVal);
                    }
                }

                this.depth -= 1;
                if (isTopScr) {
                    this.isInsideScr = false;
                }
                return returnValue;
            }

            this.hookMap[scriptName] = hooks.CreateHook<ScriptDelegate>(Detour, script->Functions->Function)!;
            this.hookMap[scriptName].Activate();
        }
    }

    private string GetString(RValue* val) {
        if (this.rnsReloadedRef == null || !this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            return string.Empty;
        }
        var ret = rnsReloaded.GetString(val);
        if (val->Type == RValueType.String) {
            ret = "\"" + ret + "\"";
        }
        return ret;
    }

    private string PrintHook(string name, int argc, RValue** argv) {
        if (this.rnsReloadedRef == null || !this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            return string.Empty;
        }
        if (argc == 0) {
            return $"{name}()";
        }
        var args = new List<string>();
        if (name == "scrit_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (item)argv[i]->Int64;
                args.Add($"item.{member}");
                if (member == item.type) {
                    args.Add($"{nameof(itemType)}.{(itemType)argv[i+1]->Int64}");
                } else if (member == item.lootHbDispType) {
                    args.Add($"{nameof(lootHbType)}.{(lootHbType)argv[i+1]->Int64}");
                } else if (member == item.evoIcon) {
                    args.Add($"evoIcon.{(elements)argv[i+1]->Int64}");
                } else if (member == item.element) {
                    args.Add($"{nameof(elements)}.{(elements)argv[i+1]->Int64}");
                } else if (member == item.treasureType) {
                    args.Add($"{nameof(treasureType)}.{(treasureType)argv[i+1]->Int64}");
                } else if (member == item.color || member == item.color2) {
                    args.Add(Rgb64ToHex((long)argv[i+1]->Real));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scrhbs_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (hotbarStatus)argv[i]->Int64;
                args.Add($"{nameof(hotbarStatus)}.{member}");
                if (member == hotbarStatus.refreshType) {
                    args.Add($"{nameof(hbsRefreshType)}.{(hbsRefreshType)argv[i+1]->Int64}");
                } else if (member == hotbarStatus.targetType) {
                    args.Add($"{nameof(hbsTargetType)}.{(hbsTargetType)argv[i+1]->Int64}");
                } else if (member == hotbarStatus.floatingTextType) {
                    args.Add($"{nameof(hbsFloatingTextType)}.{(hbsFloatingTextType)argv[i+1]->Int64}");
                } else if (member == hotbarStatus.color) {
                    args.Add(Rgb64ToHex((long)argv[i+1]->Real));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scrit_data_hitbox") {
            for (var i = 0; i < argc; i += 2) {
                var member = (hitbox)argv[i]->Int64;
                args.Add($"{nameof(hitbox)}.{member}");
                if (member == hitbox.weaponType) {
                    args.Add($"{nameof(weaponType)}.{(weaponType)argv[i+1]->Int64}");
                } else if (member == hitbox.chargeType) {
                    args.Add($"{nameof(chargeTypes)}.{(chargeTypes)argv[i+1]->Int64}");
                } else if (member == hitbox.hbInput) {
                    args.Add($"{nameof(hitboxInput)}.{(hitboxInput)argv[i+1]->Int64}");
                } else if (member == hitbox.condFunc0) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*argv[i+1]])!.Value.ToString();
                    args.Add(scrname);
                } else if (member == hitbox.condFunc1) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*argv[i+1]])!.Value.ToString();
                    args.Add(scrname);
                } else if (member == hitbox.color0 || member == hitbox.color1) {
                    args.Add(Rgb64ToHex((long)argv[i+1]->Real));
                } else if (member == hitbox.flags) {
                    args.Add("0b" + Convert.ToString((long)argv[i+1]->Real, 2));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scrit_data_stat") {
            for (var i = 0; i < argc; i += 2) {
                var member = (stat)argv[i]->Int64;
                args.Add($"{nameof(stat)}.{member}");
                if (member == stat.hbsFlag || member == stat.hbShineFlag) {
                    args.Add("0b" + Convert.ToString((long)argv[i+1]->Real, 2));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scrit_data_cdinfo") {
            for (var i = 0; i < argc; i += 2) {
                var member = (cdInfo)argv[i]->Int64;
                args.Add($"{nameof(cdInfo)}.{member}");
                if (member == cdInfo.cooldownType) {
                    args.Add($"{nameof(cdType)}.{(cdType)argv[i+1]->Int64}");
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scrit_data_trig" || name == "scrhbs_data_trig" || name == "scrtr_data_trig") {
            args.Add($"{nameof(trgType)}.{(trgType)argv[0]->Int64}");
            for (var i = 1; i < argc; i ++) {
                var arg = argv[i];
                var script = rnsReloaded.GetScriptData((int)arg->Real - 100000);
                if (script != null) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*arg])!.Value.ToString();
                    args.Add(scrname);
                } else {
                    args.Add(this.GetString(arg));
                }
            }
        } else if (name == "trigger_qpatt") {
            var script_name = this.GetString(argv[0]);
            args.Add(script_name);
            for (var i = 1; i < argc; i += 2) {
                var arg = argv[i];
                var script = rnsReloaded.GetScriptData((int)arg->Real - 100000);
                if (script != null) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*arg])!.Value.ToString();
                    args.Add(scrname);
                } else if (script_name.Contains("tpat_hb_charge")) {
                    var str1 = this.GetString(arg);
                    args.Add(str1);
                    if (str1 == "\"type\"") {
                        var member = (chargeTypes)argv[i+1]->Int64;
                        args.Add($"{nameof(chargeTypes)}.{member}");
                    } else {
                        args.Add(this.GetString(argv[i+1]));
                    }
                } else if (script_name.Contains("stat")) {
                    var str1 = this.GetString(arg);
                    args.Add(str1);
                    if (str1 == "\"stat\"") {
                        var member = (stat)argv[i+1]->Int64;
                        args.Add($"{nameof(stat)}.{member}");
                    } else if (str1 == "\"flag\"") {
                        args.Add("0b" + Convert.ToString((long)argv[i+1]->Real, 2));
                    } else if (str1 == "\"calc\"") {
                        var member = (statChangerCalc)argv[i+1]->Int64;
                        args.Add($"{nameof(statChangerCalc)}.{member}");
                    } else {
                        args.Add(this.GetString(argv[i+1]));
                    }
                } else if (script_name.Contains("flag")) {
                    var str1 = this.GetString(arg);
                    args.Add(str1);
                    if (str1 == "\"flag\"") {
                        args.Add("0b" + Convert.ToString((long)argv[i+1]->Real, 2));
                    } else {
                        args.Add(this.GetString(argv[i+1]));
                    }
                } else {
                    args.Add(this.GetString(argv[i]));
                    if (i + 1 < argc) args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "trigger_patt") {
            var script_name = this.GetString(argv[0]);
            args.Add(script_name);
            for (var i = 1; i < argc; i += 2) {
                var arg = argv[i];
                var script = rnsReloaded.GetScriptData((int)arg->Real - 100000);
                if (script != null) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*arg])!.Value.ToString();
                    args.Add(scrname);
                } else if (script_name.Contains("ipat_lullaby_harp")) {
                    var str1 = this.GetString(arg);
                    args.Add(str1);
                    if (str1 == "\"type\"") {
                        var member = (weaponType)argv[i+1]->Int64;
                        args.Add($"{nameof(weaponType)}.{member}");
                    } else {
                        args.Add(this.GetString(argv[i+1]));
                    }
                } else {
                    args.Add(this.GetString(argv[i]));
                    if (i + 1 < argc) args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "trigger_cond") {
            var script_name = this.GetString(argv[0]);
            args.Add(script_name);
            if (script_name.Contains("flag") && argv[1]->Type == RValueType.Real) {
                args.Add("0b" + Convert.ToString((long)argv[1]->Real, 2));
            } else if (script_name.Contains("equal") || script_name.Contains("unequal")) {
                var (str1, str2) = this.ParseParams(argv[1], argv[2]);
                args.Add(str1);
                args.Add(str2);
            } else if (script_name.Contains("eval")) {
                var (str1, str2) = this.ParseParams(argv[1], argv[3]);
                args.Add(str1);
                args.Add(this.GetString(argv[2]));
                args.Add(str2);
            } else {
                for (var i = 1; i < argc; i += 1) {
                    var arg = argv[i];
                    var script = rnsReloaded.GetScriptData((int)arg->Real - 100000);
                    if (script != null) {
                        var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*arg])!.Value.ToString();
                        args.Add(scrname);
                    } else {
                        args.Add(this.GetString(argv[i]));
                    }
                }
            }
        } else if (name == "trigger_set") {
            var script_name = this.GetString(argv[0]);
            args.Add(script_name);
            for (var i = 1; i < argc; i += 1) {
                var arg = argv[i];
                var script = rnsReloaded.GetScriptData((int)arg->Real - 100000);
                if (script != null) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*arg])!.Value.ToString();
                    args.Add(scrname);
                } else if (script_name.Contains("flags") && arg->Type == RValueType.Real) {
                    args.Add("0b" + Convert.ToString((long)arg->Real, 2));
                } else if (script_name.Contains("tset_uservar_hb_cooldownvar") && i == 3) {
                    var member = (cdInfo)argv[i]->Int64;
                    args.Add($"{nameof(cdInfo)}.{member}");
                } else if (script_name.Contains("tset_uservar_hb_hitboxvar") && i == 3) {
                    var member = (hitbox)argv[i]->Int64;
                    args.Add($"{nameof(hitbox)}.{member}");
                } else if (script_name.Contains("tset_uservar_hb_itemvar") && i == 3) {
                    var member = (item)argv[i]->Int64;
                    args.Add($"{nameof(item)}.{member}");
                } else if (script_name.Contains("tset_uservar_hb_stat") && i == 3) {
                    var member = (stat)argv[i]->Int64;
                    args.Add($"{nameof(stat)}.{member}");
                } else if (script_name.Contains("tset_uservar_player_stat") && i == 4) {
                    var member = (stat)argv[i]->Int64;
                    args.Add($"{nameof(stat)}.{member}");
                } else {
                    args.Add(this.GetString(argv[i]));
                }
            }
        } else if (name == "trigger_target") {
            var script_name = this.GetString(argv[0]);
            args.Add(script_name);
            var next_index = 1;
            // TODO various flag things (ttrg_player_prune_hbsflag, ttrg_players_opponent_binary, etc)
            if (script_name.Contains("ttrg_hotbarslots")) {
                if (script_name.EndsWith("prune")) {
                    var (str1, str2) = this.ParseParams(argv[1], argv[3]);
                    args.Add(str1);
                    args.Add(this.GetString(argv[2]));
                    args.Add(str2);
                    next_index = 4;
                } else if (script_name.EndsWith("cdtype") || script_name.EndsWith("cdstat")) {
                    var member = (cdType)argv[1]->Int64;
                    args.Add($"{nameof(cdType)}.{member}");
                    next_index = 2;
                } else if (script_name.EndsWith("hitboxstat")) {
                    var member = (hitbox)argv[1]->Int64;
                    args.Add($"{nameof(hitbox)}.{member}");
                    next_index = 2;
                } else if (script_name.EndsWith("itemstat")) {
                    var member = (item)argv[1]->Int64;
                    args.Add($"{nameof(item)}.{member}");
                    next_index = 2;
                } else if (script_name.Contains("weapontype")) {
                    var member = (weaponType)argv[1]->Int64;
                    args.Add($"{nameof(weaponType)}.{member}");
                    next_index = 2;
                }
            }
            for (var i = next_index; i < argc; i++) {
                var arg = argv[i];
                args.Add(this.GetString(arg));
            }
        } else {
            for (var i = 0; i < argc; i++) {
                var arg = argv[i];
                var script = rnsReloaded.GetScriptData((int)arg->Real - 100000);
                if (script != null) {
                    var scrname = rnsReloaded.ExecuteCodeFunction("script_get_name", null, null, [*arg])!.Value.ToString();
                    args.Add(scrname);
                } else {
                    args.Add(this.GetString(arg));
                }
            }
        }

        var retVal = $"{name}({string.Join(", ", args)})";
        return retVal;
    }

    private static string Rgb64ToHex(long value) {
        byte r = (byte)(value & 0xFF);
        byte g = (byte)((value >> 8) & 0xFF);
        byte b = (byte)((value >> 16) & 0xFF);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private (string, string) ParseParams(RValue* arg1, RValue* arg2) {
        if ((arg1->Type == RValueType.String && arg2->Type == RValueType.String)
        || (arg1->Type != RValueType.String && arg2->Type != RValueType.String)) {
            return (
                this.GetString(arg1),
                this.GetString(arg2)
            );
        }
        string param_str, num_str;
        long param_num;
        if (arg1->Type == RValueType.String) {
            param_str = this.GetString(arg1);
            if (arg2->Type == RValueType.Real) {
                param_num = (long)arg2->Real;
            } else {
                param_num = arg2->Int64;
            }
            num_str = this.GetString(arg2);
        } else {
            param_str = this.GetString(arg2);
            if (arg1->Type == RValueType.Real) {
                param_num = (long)arg1->Real;
            } else {
                param_num = arg1->Int64;
            }
            num_str = this.GetString(arg1);
        }

        if (param_str.EndsWith("cooldownType")) {
            num_str = $"{nameof(cdType)}.{(cdType)param_num}";
        } else if (param_str.Contains("weaponType")) {
            num_str = $"{nameof(weaponType)}.{(weaponType)param_num}";
        } else if (param_str.Contains("hbInput")) {
            num_str = $"{nameof(hitboxInput)}.{(hitboxInput)param_num}";
        } else if (param_str.Contains("flags")) {
            num_str = "0b" + Convert.ToString(param_num, 2);
        }

        if (arg1->Type == RValueType.String) {
            return (param_str, num_str);
        } else {
            return (num_str, param_str);           
        }
    }

    private void CreateFuncHook(String funcName) {
        if (
            this.rnsReloadedRef != null
            && this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)
            && this.hooksRef != null
            && this.hooksRef.TryGetTarget(out var hooks)
        ) {
            var funcInt = rnsReloaded.CodeFunctionFind(funcName);
            if (funcInt == null) {
                this.logger.PrintMessage($"Failed to find code function {funcName}!", this.logger.ColorRed);
                return;
            }
            var addr = (nint)rnsReloaded.GetTheFunction(funcInt.Value).Routine;


            void Detour(
                RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
            ) {
                var hook = this.hookFuncMap[funcName];
                if (!this.isInsideScr || !this.shouldDumpFuncs) {
                    hook!.OriginalFunction(returnValue, self, other, argc, argv);
                    return;
                }

                // Write call
                var call = this.PrintFuncHook(funcName, argc, argv);
                using (var writer = File.AppendText(this.dumpFilename)) {
                    var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
                    writer.WriteLine(pre + call);
                }

                this.depth += 1;
                hook!.OriginalFunction(returnValue, self, other, argc, argv);

                // Write return value
                var retString = rnsReloaded.GetString(returnValue);
                if (funcName == "merge_color") {
                    retString = Rgb64ToHex((long)returnValue->Real);
                }
                if (retString != "undefined") {
                    var retVal = $"-> {retString}";
                    using (var writer = File.AppendText(this.dumpFilename)) {
                        var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
                        writer.WriteLine(pre + retVal);
                    }
                }

                this.depth -= 1;
            }

            this.hookFuncMap[funcName] = hooks.CreateHook<RoutineDelegate>(Detour, addr);
            this.hookFuncMap[funcName].Activate();
        }
    }

    private string PrintFuncHook(string name, int argc, RValue* argv) {
        if (this.rnsReloadedRef == null || !this.rnsReloadedRef.TryGetTarget(out var rnsReloaded)) {
            return string.Empty;
        }
        if (argc == 0) {
            return $"{name}()";
        }
        var args = new List<string>();
        if (name == "merge_color") {
            args.Add(Rgb64ToHex((long)argv[0].Real));
            args.Add(Rgb64ToHex((long)argv[1].Real));
            args.Add(this.GetString(&argv[2]));
        } else {
            for (var i = 0; i < argc; i++) {
                var arg = argv[i];
                if (arg.ToString().Contains("ds_list")) {
                    args.Add(this.PrintList(&arg));
                } else {
                    args.Add(this.GetString(&arg));
                }
            }
        }

        var retVal = $"{name}({string.Join(", ", args)})";
        return retVal;
    }

    private RValue* gameSpeedDetour(
        CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        // (*argv)->Real *= this.config.SpeedMultiplier;
        returnValue = this.gameSpeedHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* encounterStartDetour(
     CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            // rnsReloaded.ExecuteScript("scrbp_gamespeed", self, other, [new RValue(1.0)]);
            // var len = rnsReloaded.ExecuteCodeFunction("ds_list_size", null, null, [new RValue(606)])!.Value.Real;
            // var outstring = "";
            // for (var i = 0; i < len; i++) {
            //     var el = rnsReloaded.ExecuteCodeFunction("ds_list_find_value", null, null, [
            //         new RValue(606),
            //         new RValue(i)
            //     ])!.Value;
            //     outstring += el.ToString() + ",";
            // }
            // Console.WriteLine(outstring);
            // this.findRunMenuStuff();
            this.dumpRooms($"dump/rooms{++this.dumpIndex}.json");
            foreach (var str in new List<string> {
                                "arsenal",
                                "asha",
                                "aura",
                                "avy",
                                "bloom",
                                "butterfly0",
                                "butterfly1",
                                "butterfly3",
                                "depths",
                                "kingdom",
                                "lakeside",
                                "lighthouse",
                                "matti",
                                "mell",
                                "merran",
                                "moon",
                                "nest",
                                "outskirts",
                                "ranalie",
                                "rings",
                                "sanct",
                                "seal",
                                "shira",
                                // "speaker",
                                "spell",
                                "sphere",
                                "star",
                                "streets",
                                "twili",
                                "witch"
                                }) {
                var strR = rnsReloaded.utils.CreateString(str)!.Value;
                rnsReloaded.ExecuteScript("scr_dialog_handle_flag_color", null, null, [new RValue(0.0), strR]);
            }
            Console.WriteLine("Finished dialoging");

            // TODO: check notches later in run, might contain seed for fight specific rng(?) (crits, variance, etc)
        }
        returnValue = this.encounterStartHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* chooseHallsDetour(
     CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        // Console.WriteLine(this.itemData->Get(489)->Get(0)->Get(2)->ToString());
        // Console.WriteLine(this.itemData->Type);
        // Crashes at around item 500 for some reason, I just do it in two runs/compilations
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            // using (StreamWriter writer = File.CreateText("dump_item_data.txt")) {
            //     var i = 0;
            //     var len = rnsReloaded.ArrayGetLength(this.itemData)!.Value.Real;
            //     while (i < len) {
            //         writer.WriteLine(this.itemData->Get(i)->ToString());
            //         i++;
            //     }
            // }
            // using (StreamWriter writer = File.CreateText("dump_enemy_data.txt")) {
            //     var i = 0;
            //     var len = rnsReloaded.ArrayGetLength(this.enemyData)!.Value.Real;
            //     while (i < len) {
            //         writer.WriteLine(this.enemyData->Get(i)->ToString());
            //         i++;
            //     }
            // }
        }
        returnValue = this.chooseHallsHook!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private RValue* hbladePrim(
     CInstance* self, CInstance* other, RValue* returnValue, int argc, RValue** argv
    ) {
        this.dumpRooms($"dump/rooms{++this.dumpIndex}.json");
        returnValue = this.hookMap["ipat_hblade_0"]!.OriginalFunction(self, other, returnValue, argc, argv);
        return returnValue;
    }

    private string PrintList(RValue* list) {
        if (!this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            return "";
        }
        if (this.hookFuncMap.ContainsKey("ds_list_size")) {
            this.hookFuncMap["ds_list_size"].Disable();
        }if (this.hookFuncMap.ContainsKey("ds_list_find_value")) {
            this.hookFuncMap["ds_list_find_value"].Disable();
        }
        // var list_id = list->Real;
        // Console.WriteLine($"{list->Real}, {list->Int64}, {list->ToString()}");
        var len = rnsReloaded.ExecuteCodeFunction("ds_list_size", null, null, [*list])!.Value.Real;
        var type = list->Type;
        // var outstring = $"str={list->ToString()},idtype={type},len={len},list=";
        var outstring = $"list={list->ToString()},len={len},list=[";
        for (var i = 0; i < len; i++) {
            var el = rnsReloaded.ExecuteCodeFunction("ds_list_find_value", null, null, [
                *list,
                new RValue(i)
            ])!.Value;
            outstring += this.GetString(&el);
            if (i+1 < len) {
                outstring += ",";
            }
        }
        outstring += "]";
        if (this.hookFuncMap.ContainsKey("ds_list_size")) {
            this.hookFuncMap["ds_list_size"].Enable();
        }if (this.hookFuncMap.ContainsKey("ds_list_find_value")) {
            this.hookFuncMap["ds_list_find_value"].Enable();
        }
        return outstring;
        // Console.WriteLine(outstring);
    }
    private void DumpLists(IRNSReloaded rnsReloaded) {
        using (StreamWriter writer = File.AppendText($"dump_ds_lists.txt")) {
            for (var i = 0.0; i < 700.0; i++) {
                var list_id = new RValue(i);
                var outstr = this.PrintList(&list_id);
                writer.WriteLine(outstr);
            }
        }
    }

    private void DSListShuffleDetour(
        RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
    ) {
        this.dsListShuffleHook!.OriginalFunction(returnValue, self, other, argc, argv);
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded)) {
            var liststr = this.PrintList(argv);
            var seed = (long)rnsReloaded.ExecuteCodeFunction("random_get_seed", null, null, [])!.Value.Real;
            using (StreamWriter writer = File.AppendText($"setseed-{seed}.txt")) {
                writer.WriteLine($"Shuffling, list_id: {argv->ToString()}");
                writer.WriteLine(liststr);
            }
            // if (liststr.Contains("enc")) {
            //     // this.DumpLists(rnsReloaded);
            //     var mapSeedR = rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "mapSeed");
            //     Console.WriteLine($"Seed: {mapSeedR->ToString()}");
            //     Console.WriteLine(liststr);

            // }
        }
    }

    private unsafe void DSListCopyDetour(
        RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
    ) {
        this.dsListCopyHook!.OriginalFunction(returnValue, self, other, argc, argv);
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded))
        {
            // var seed = (long)rnsReloaded.ExecuteCodeFunction("random_get_seed", null, null, [])!.Value.Real;
            using (StreamWriter writer = File.AppendText($"ds-list-copies.txt")) {
                writer.WriteLine($"ds_list_copy({argv->Int64},{(argv+1)->Int64})");
            }
        }
    }

    private void RandomSetSeedDetour(
        RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
    ) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded))
        {
            if (argv->Real == 1521834084.0) {

            }
            var mapSeedR = rnsReloaded.FindValue(rnsReloaded.GetGlobalInstance(), "mapSeed");
            if (mapSeedR->Real == argv->Real) {
                argv->Real = 1585;
                mapSeedR->Int64 = 1585;
                mapSeedR->Type = RValueType.Int64;
                this.mapSeedR = (long)argv->Real;
                Console.WriteLine($"Setting mapseed to {argv->Real}");
                // using (StreamWriter writer = File.CreateText($"setseed-{this.mapSeedR}.txt")) {}
            }
            this.randomSetSeedHook!.OriginalFunction(returnValue, self, other, argc, argv);
        }
    }

    private double RandomDetour(double upper) {
        var ret = this.randomHook!.OriginalFunction(upper);
        if (!this.isInsideScr || !this.shouldDumpFuncs) {
            return ret;
        }

        var retVal = $"random({upper}) -> {ret}";
        using (var writer = File.AppendText(this.dumpFilename)) {
            var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
            writer.WriteLine(pre + retVal);
        }

        return ret;
    }

    private double RandomRangeDetour(double lower, double upper) {
        var ret = this.randomRangeHook!.OriginalFunction(lower, upper);
        if (!this.isInsideScr || !this.shouldDumpFuncs) {
            return ret;
        }

        var retVal = $"random_range({lower}, {upper}) -> {ret}";
        using (var writer = File.AppendText(this.dumpFilename)) {
            var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
            writer.WriteLine(pre + retVal);
        }

        return ret;
    }

    private double IRandomDetour(long upper) {
        var ret = this.irandomHook!.OriginalFunction(upper);
        if (!this.isInsideScr || !this.shouldDumpFuncs) {
            return ret;
        }

        var retVal = $"irandom({upper}) -> {ret}";
        using (var writer = File.AppendText(this.dumpFilename)) {
            var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
            writer.WriteLine(pre + retVal);
        }

        return ret;
    }

    private double IRandomRangeDetour(long lower, long upper) {
        var ret = this.irandomRangeHook!.OriginalFunction(lower, upper);
        if (!this.isInsideScr || !this.shouldDumpFuncs) {
            return ret;
        }

        var retVal = $"irandom_range({lower}, {upper}) -> {ret}";
        using (var writer = File.AppendText(this.dumpFilename)) {
            var pre = String.Concat(Enumerable.Repeat("  ", this.depth));
            writer.WriteLine(pre + retVal);
        }

        return ret;
    }

    private void ChooseDetour(
        RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
    ) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded))
        {
            var seed = (long)rnsReloaded.ExecuteCodeFunction("random_get_seed", null, null, [])!.Value.Real;
            var outstring = "";
            for (var i = 0; i < argc; i++) {
                outstring += (argv+i)->ToString() + ",";
            }
            using (StreamWriter writer = File.AppendText($"setseed-{seed}.txt")) {
                writer.WriteLine($"choose({outstring}), argc={argc}");
            }
        }
        this.chooseHook!.OriginalFunction(returnValue, self, other, argc, argv);
    }

    private void ArrayShuffleDetour(
        RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
    ) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded))
        {
            var seed = (long)rnsReloaded.ExecuteCodeFunction("random_get_seed", null, null, [])!.Value.Real;
            // var len = rnsReloaded.ArrayGetLength(argv)!.Value.Real;
            // var outstring = "[";
            // for (int i = 0; i < len; i++) {
            //     outstring +=
            // }
            using (StreamWriter writer = File.AppendText($"setseed-{seed}.txt")) {
                if (argc == 1) {
                    writer.WriteLine($"array_shuffle({argv->ToString()})");
                } else if (argc == 2) {
                    writer.WriteLine($"array_shuffle({argv->ToString()},{(argv+1)->Int64})");
                } else {
                    writer.WriteLine($"array_shuffle({argv->ToString()},{(argv+1)->Int64},{(argv+2)->Int64}), argc={argc}");
                }
            }
        }
        this.arrayShuffleHook!.OriginalFunction(returnValue, self, other, argc, argv);
    }

    private void ArrayShuffleExtDetour(
        RValue* returnValue, CInstance* self, CInstance* other, int argc, RValue* argv
    ) {
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded))
        {
            var seed = (long)rnsReloaded.ExecuteCodeFunction("random_get_seed", null, null, [])!.Value.Real;
            // var len = rnsReloaded.ArrayGetLength(argv)!.Value.Real;
            // var outstring = "[";
            // for (int i = 0; i < len; i++) {
            //     outstring +=
            // }
            using (StreamWriter writer = File.AppendText($"setseed-{seed}.txt")) {
                if (argc == 1) {
                    writer.WriteLine($"array_shuffle_ext({argv->ToString()})");
                } else if (argc == 2) {
                    writer.WriteLine($"array_shuffle_ext({argv->ToString()},{(argv+1)->Int64})");
                } else {
                    writer.WriteLine($"array_shuffle_ext({argv->ToString()},{(argv+1)->Int64},{(argv+2)->Int64}), argc={argc}");
                }
            }
        }
        this.arrayShuffleExtHook!.OriginalFunction(returnValue, self, other, argc, argv);
    }

    public void Resume() { }
    public void Suspend() { }
    public bool CanSuspend() => true;

    public void Unload() { }
    public bool CanUnload() => false;

    public Action Disposing => () => { };
}
