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
using System.Text.RegularExpressions;

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
    // NOTE: remove gml_Object_Dataloader and scr_hexchar_to_int and rgb when getting new list of funcs
    private readonly List<string> dumpScripts = [
        "scrdtit_polar_coat", "scrdtmv_druid_2_defstat", "scrdtit_wolf_hood", "scrdtit_vorpal_dao", "scrdtit_hermes_bow", "scrdtit_nightguard_gloves", "scrdtpln_line", "screnc_data_pattern_ext", "scrdtit_midsummer_dress", "scrdtit_hydrous_blob", "scrdtit_clay_rabbit", "scranim_data_set_external_simple", "scrdtmv_gunner_3_deftrig", "scrdtmv_hammer_3_deftrig", "scrdtit_raven_grimoire", "scrdtit_phantom_dagger", "scrdtit_stormdance_gown", "scrdtit_sun_pendant", "scrpj_bullet_data_row", "scranim_data_headoffx", "scrdt_dialog_entry", "scrdt_quest_deco", "scrdtit_dynamite_staff", "scrdtmv_spsword_3_deftrig", "scrdtmv_sniper_1_defstat", "scrdtenc_streets", "scr_readsheet_populate_possible_halls", "scrdtmv_druid_1_defstat", "scrdtit_starry_cloak", "scrdtit_volcano_spear", "scrdtit_seashell_shield", "scrdtit_sacred_shield", "scr_encounter_internal_data", "scr_projectile_internal_data", "scrdtmv_hblade_0", "scrdtit_pajama_hat", "scrdtit_meteor_staff", "scrdtit_peridot_rapier", "scrdtit_altair_dagger", "scrdt_quest_dialog", "scrdtit_rusted_greatsword", "scrdtmv_bruiser_0_defstat", "scrdtit_mermaid_scale", "scrdtit_boulder_shield", "sfxset_player_hblade", "scrdtanim_enemy_sanct", "scranim_data_set_external", "scrdtit_spinning_chakram", "scrdtit_flame_bow", "scrdtit_falconfeather_dagger", "sfxset_dark", "scrdt_quest_minstoryclear", "scrdtmv_dancer_3", "scrdtit_stonebreaker_staff", "scrext_trg_replace", "scrdtmv_hblade_3_defstat", "scrdtit_onepiece_swimsuit", "scrdtmv_dancer_2_deftrig", "scrdtmv_defender_3_deftrig", "scrdtmv_wizard_1_deftrig", "scrdtmv_shadow_1_defstat", "scrdtit_unknown_item", "scr_readsheet_pattern_timing", "scr_item_data_set_transformids", "scrdt_quest_shopreq", "scr_mod_refresh_lists", "trigger_patt", "scrdtmv_druid_3", "scrdtit_raindrop_earrings", "screnc_data_set", "scrdtanim_enemy_wolf", "scrdtit_phoenix_charm", "scrdtit_raiju_crown", "screnc_data_set_external", "scrdtqu_credits", "scrdtmv_pyro_3", "scrdtmv_gunner_0", "scrdtit_darkcloud_necklace", "scrdtit_darkmage_charm", "scrdtit_smoke_shield", "scrdtit_teacher_knife", "sfxset_dark2", "scr_hbstatus_internal_data", "scrit_data_set_external", "scrdtit_icicle_earrings", "scrdtit_heavens_codex", "scrdten_training_dummy", "scrdtmv_hammer_3_defstat", "scrdtit_demon_horns", "scrdtmv_hblade_1_deftrig", "scrdtit_firescale_corset", "scrdtit_blue_rose", "scrdtit_poisonfrog_charm", "scrdtit_waterfall_polearm", "scritex_set_type", "scrdtit_cavers_cloak", "scrdtmv_hblade_2_deftrig", "scrdtmv_assassin_2_deftrig", "scrdtit_sleeping_greatbow", "scrdtit_winged_cap", "scrdtit_lion_charm", "scrtr_data_stat", "scrdthbs_order", "scranim_data_attanim", "scrhwext_data_set_local", "scrdtit_comforting_coat", "scrdtit_hexed_blindfold", "scrdtit_fossil_dagger", "scrdtenc_lighthouse", "scrdtenc_aurum", "scrdtit_large_umbrella", "scr_move_internal_data", "scrdtmv_gunner_2_defstat", "scrdtmv_shadow_2_emerald_trig", "scrdtit_black_wakizashi", "scrdtit_royal_staff", "scrdtit_deathcap_tome", "scrdtit_timespace_dagger", "scrdtit_youkai_bracelet", "scrdtpdn_donut", "scrdtit_ribboned_staff", "scrdtit_apple_plate", "scrdtit_book_of_cheats", "sfxdt_data_set", "scrdtit_sweet_taffy", "scrdtmv_assassin_2_defstat", "scrdtmv_hammer_1_deftrig", "scrdtit_thunderclap_gloves", "sfxset_empty", "scranim_data_rotation", "scranim_data_set_external_headoffy", "scritex_set_chargetype", "scrdt_quest_npc_e", "scrdtqu_hearts", "scrdtit_witchs_cloak", "scrdtit_shinobi_tabi", "scrdtit_whiteflame_staff", "scrdtit_desert_earrings", "scrtr_data_trig", "scr_mod_debug", "scrdtqu_beasts", "scrdtqu_spell", "scrdtit_straw_hat", "scrdtmv_dancer_0_defstat", "scrdtit_killing_note", "scrdtit_tactician_rod", "scrdtqu_dragon", "object_get_depth", "scrdtmv_wizard_3", "scrit_data_set", "scrdtit_twinstar_earrings", "sfxset_blade", "scr_readsheet_internal", "scrdtit_vanilla_wafers", "scrdtmv_dancer_0_deftrig", "scrdtit_darkstorm_knife", "scral_data_color", "scrdtpjl_laser", "scr_readsheet_populate_possible_items", "scrdtmv_druid_1", "scrdtmv_spsword_1_deftrig", "scrdtit_spiked_shield", "scrhwext_data_set_notch", "scrdtmv_defender_1_defstat", "scrdtit_firststrike_bracelet", "scrdthbs_stoneskin", "scrdthbs_curse", "scr_readsheet_pattern_new", "scrdtmv_sniper_0", "scrdtmv_wizard_1_defstat", "scrdtmv_sniper_3_deftrig", "scrdtit_vampiric_dagger", "scrdtit_holy_greatsword", "scrdtit_nova_crown", "scr_trinket_internal_data_dlc", "scr_randomizeditem_set_ids", "scrdtmv_druid_0", "trigger_cond", "trigger_lpatt", "scrdtmv_shadow_3_deftrig", "scrdtmv_assassin_1_deftrig", "scrdtit_haunted_gloves", "scrdthbs_haste", "scrdtmv_gunner_1_deftrig", "scrdtit_lancer_gauntlets", "scrdtit_watermage_pendant", "scrdtpjb_fire", "scrdthbs_burn", "scrdtmv_assassin_3", "scrdtmv_none_2", "scrdtit_amethyst_bracelet", "scr_readsheet_items", "scrdtit_staff_of_sorrow", "scrdtmv_ancient_2_defstat", "scrdtit_emerald_chestplate", "scrdtit_occult_dagger", "scritex_trig_arr", "scrit_data_cdinfo", "scrdtmv_hblade_2", "sfxset_player_druid", "scrdten_depths", "scrdtmv_dancer_3_defstat", "scrdtmv_shadow_0", "scrdtmv_pyro_2_defstat", "scrdtmv_shadow_2_defstat", "scrdtit_sawtooth_cleaver", "scrdtmv_sniper_0_deftrig", "scrdtit_blackhole_charm", "scrdtit_frost_dagger", "scrdtit_sand_shovel", "scrdtit_sparrow_feather", "scrdthbs_flutterstep", "scranim_data_set", "scrdtanim_enemy_depths", "scrdtit_frozen_staff", "scrdtit_wind_spear", "scrdtmv_pyro_1_defstat", "scrdtit_butterfly_hairpin", "scrdtqu_fey", "scrdtit_lonesome_pendant", "scrdtmv_hammer_2", "scrdtit_garnet_staff", "scrdtpjb_light2", "sfxset_player_dancer", "scrdtanim_enemy_geode", "scranim_data_set_external_headoffx", "scrdtit_colorful_earrings", "scrdtit_crown_of_swords", "scrdtmv_druid_3_defstat", "scrdtmv_sniper_3_defstat", "scrdtmv_ancient_3_deftrig", "scrdtit_golems_claymore", "scrdtmv_defender_2_defstat", "scrdtit_queens_crown", "scrdtit_marble_clasp", "scrdtit_ruins_sword", "scrdtmv_bruiser_2_deftrig", "scrdtmv_gunner_1", "scr_pickup_internal_data", "scrhbs_data_elm", "scrit_data_elm", "scrdtmv_hammer_0_deftrig", "scrdtmv_pyro_0_deftrig", "scrdtit_brightstorm_spear", "sfxset_player_wizard", "scrdtmv_assassin_1", "scrdtit_timemage_cap", "scrdtit_eaglewing_charm", "scrdtit_bolt_staff", "scrdtit_battlemaiden_armor", "scrdtanim_lq_auto", "scrdtmv_assassin_0_defstat", "scrdtmv_shadow_1", "scrhw_data_elm", "scr_readsheet_populate_trgfunctions", "scritex_read_trigger", "scrdtit_spear_of_remorse", "scrdtmv_wizard_2_deftrig", "scrdtmv_bruiser_3", "scrdtit_vega_spear", "scrdtpjb_dark2", "scrdtit_ninjutsu_scroll", "scrdtenc_nest", "scr_readsheet_external", "scrdtit_bluebolt_staff", "sfxset_player_pyro", "scr_readsheet_color", "scrhbsex_set_refreshtype", "scranim_data_headoffy", "scrdtmv_bruiser_3_deftrig", "sfxdt_data_row", "scrit_elmret", "scrdtmv_defender_0", "scrdtit_feathered_overcoat", "screnc_data_pattern", "sfxset_player_bruiser", "subarray_2d_simple", "scrhbsex_set_targettype", "scrdtit_righthand_cast", "scrdtmv_wizard_0_deftrig", "scrdtit_blacksteel_buckler", "scrdtit_darkglass_spear", "scr_ally_internal_data", "scrdthbs_sniper", "scrdtqu_saya", "scrdtit_crown_of_love", "scrdtit_saltwater_staff", "scrdtmv_druid_1_deftrig", "scrdtmv_ancient_0_defstat", "scrdtmv_wizard_0", "scrdtit_sacredstone_charm", "scrhbs_data_set_ext", "scrdtit_rosered_leotard", "scrdtit_hells_codex", "scrdten_dragons", "scrdten_geode", "scrdtit_ghost_spear", "scritex_set_hbinput", "sfxset_light", "scrdtit_sun_sword", "scrdtmv_bruiser_1_defstat", "scrdtit_windbite_dagger", "scrdtit_kappa_shield", "scrdtpk_pickup", "scrdtpjb_water", "scrdtanim_enemy_dragon", "scrdtit_spark_of_determination", "scrdtit_fanciful_book", "scrdtmv_spsword_1_defstat", "scrdtit_sapphire_violin", "scrdthbs_ghostflame", "scrhw_data_set", "scrdt_quest_npc", "scrdtmv_druid_2_deftrig", "scrdtmv_ancient_1", "scrdtmv_ancient_1_defstat", "scrdtit_granite_greatsword", "scrdtit_kyou_no_omikuji", "scrdtenc_sanct", "scrpj_line_data_row", "scrpj_donut_data_row", "scrdthbs_lucky", "screnc_data_set_replace_local", "scrit_data_hitbox", "scrdtit_sewing_sword", "scrdtit_sharpedged_shield", "scr_enemy_internal_data", "scr_read_dialog_data", "scrdtit_handmade_charm", "scren_data_elm", "scrdtmv_bruiser_2_defstat", "scrdtmv_pyro_3_deftrig", "scrdtit_redwhite_ribbon", "scrdtit_crane_katana", "scral_data_elm", "scritex_set_cdtype", "scrdtmv_dancer_2", "scrdtmv_pyro_1", "scrdtit_rockdragon_mail", "scrdthbs_bleed", "scrdthbs_frogs", "scrdthbs_wolf", "scrdtmv_hblade_0_defstat", "scrdtmv_ancient_0_deftrig", "scrdtit_chrome_shield", "scrdtit_storm_petticoat", "scrdtit_compound_gloves", "scrdtenc_outskirts", "scrpj_laser_data_row", "scrpj_ray_data_row", "scr_hallway_internal_data", "scr_readsheet_pattern", "scrdtit_memory_greatsword", "scrdtit_rain_spear", "scrdtmv_druid_3_deftrig", "scrdtit_snipers_eyeglasses", "scrdtit_sandpriestess_spear", "sfxset_player_defender", "scrdtqu_shopkeeper", "scr_loadsprite_clear", "scrdtit_greatsword_pendant", "scrdtmv_hammer_2_deftrig", "scrdtmv_pyro_0", "scrdtit_reddragon_blade", "scrdtit_sacred_bow", "scrdtit_venom_hood", "scrdtit_jade_staff", "scrdtit_shield_of_smiles", "scrdtit_giant_paintbrush", "scrdtmv_gunner_2_deftrig", "scrdtmv_wizard_0_defstat", "scrdtit_maid_outfit", "scrdtit_performers_shoes", "scrdtmv_druid_2", "scrdtmv_sniper_2", "scrdtmv_assassin_1_defstat", "scrdtit_hawkfeather_fan", "scrdtit_grasswoven_bracelet", "scrdtanim_enemy_aurum", "scrdtmv_hammer_0_defstat", "scrdtit_snakefang_dagger", "scrdtit_red_tanzaku", "scrdtpk_potion", "scrdtenc_keep", "scrdtanim_player", "scrdtmv_none_3", "scrdtmv_defender_1", "scrdtmv_assassin_3_defstat", "scrdtit_silver_coin", "scrdtit_sunflower_crown", "scrdtit_ivy_staff", "scrdtpjb_light", "sfxset_player_sniper", "sfxset_player_ancient_pet", "scr_readsheet_enemy", "scrdten_darkhall", "scrdtmv_shadow_1_deftrig", "scrdtanim_charselect", "scrdtmv_wizard_2_defstat", "scrdtmv_sniper_1_deftrig", "scrdtit_flamewalker_boots", "scral_data_set", "sfxset_ray", "scr_trinket_internal_data", "scren_data_set_external", "scrdtit_whitewing_bracelet", "scrdtmv_dancer_3_deftrig", "scrdtit_lullaby_harp", "screnc_data_pattern_sp", "sfxset_enemy", "scrdthbs_flow", "scrrsptrg", "scr_readsheet_anim", "scrdtqu_pets", "scrdtit_winter_hat", "scrdtmv_unknown_0", "scrdtmv_ancient_3", "scrdtit_glittering_trumpet", "scrpj_circle_data_row", "scrhbs_data_trig", "scrdtit_snow_boots", "scrdtit_tiny_fork", "scrdtmv_defender_1_deftrig", "scrdtpcr_circle", "sfxset_player_ancient", "scr_lang_get", "scrdtmv_defender_2", "scrdtit_nightingale_gown", "scrdthbs_spark", "scrdt_quest", "scrdtit_glacier_spear", "scrdtmv_gunner_3_defstat", "scrdtmv_pyro_1_deftrig", "scrdtit_calling_bell", "scrdthbs_spsword", "scrnpc_data_elm", "scrdtit_lefthand_cast", "scrdtit_hooked_staff", "scrdtmv_sniper_2_defstat", "scrdtit_darkmagic_blade", "scrdtit_shinsoku_katana", "scrdtit_gladiator_helmet", "scrdtit_tornado_staff", "scrdtit_flamedancer_dagger", "scrdtpjb_dark", "scrdtit_hidden_blade", "scrdten_mice", "scrdtmv_defender_2_deftrig", "scrdtmv_defender_3_defstat", "scrdtit_pyrite_earrings", "scrdtmv_spsword_0_defstat", "scrdtit_ruby_circlet", "scrdtenc_test", "scrdtpn_none", "scrdtit_haste_boots", "scrdthbs_freeze", "scrdtqu_birds", "scrdtmv_bruiser_3_defstat", "scrdtit_robe_of_dark", "scrdten_other", "scrdtmv_hblade_0_deftrig", "scrdtit_leech_staff", "scrdtit_golden_chime", "scrdtit_obsidian_rod", "scrdtit_reflection_shield", "scrpj_data_set", "scrdtanim_enemy_other", "scrdtit_springloaded_scythe", "scrdtmv_bruiser_0", "scrdtmv_wizard_3_defstat", "scrdtit_ninja_robe", "scrdtit_cloud_guard", "sfxset_player_gunner", "scrdtit_bladed_cloak", "scrdtmv_spsword_0", "scrdtmv_spsword_0_deftrig", "scrdtmv_spsword_1", "scrdtit_tiny_hourglass", "scranim_data_elm", "scrpj_cone_data_row", "scrdtqu_wolf", "scrdtit_strongmans_bar", "scrdtmv_sniper_2_deftrig", "scrdtmv_ancient_0", "scrdtmv_wizard_3_deftrig", "scrdtit_mimick_rabbitfoot", "scr_read_quest_data", "scrdtit_nightstar_grimoire", "scrdtit_chemists_coat", "sfxset_light2", "scrdtmv_spsword_2_defstat", "scrdtmv_defender_0_deftrig", "scrdtmv_bruiser_1", "scrdtenc_geode", "sfxset_water", "scrdthbs_paint", "scrdtanim_enemy_reflection", "scr_readsheet_pattern_instruction", "scrdt_quest_minshopvisit", "scrdt_quest_deco_flag", "scrnpc_data_set", "trigger_target", "scrdtit_jesters_hat", "scrdtmv_hammer_2_defstat", "scrdtit_necronomicon", "sfxset_player_shadow", "sfxset_item_hydra", "scrdtqu_frog", "scrdtmv_hammer_3", "csv_load", "scr_readsheet_hallway", "scrdtit_coldsteel_shield", "scrmv_elm_colors", "scrdtmv_druid_0_deftrig", "scrdtmv_none_0", "scrdtit_bloodhound_greatsword", "scrdthbs_none", "scr_npc_internal_data", "scrdtmv_dancer_2_defstat", "scrdtmv_sniper_0_defstat", "scrdtmv_druid_0_defstat", "scrdtit_opal_necklace", "scrdtit_usagi_kamen", "scrdtit_robe_of_light", "scrdtit_rainbow_cape", "scrdtit_drill_shield", "scrdtit_staticshock_earrings", "screnc_data_pattern_mp", "scrdthbs_super", "screnumkey", "scr_item_internal_data", "scrdtit_unsacred_pendant", "scrdtit_caramel_tea", "scrdten_pinnacle", "scrdtmv_dancer_1_defstat", "scrdtmv_assassin_0", "scrdtit_blackwing_staff", "scrdtit_reaper_cloak", "scrdtit_greysteel_shield", "scrdtenc_test2", "scrdtmv_hblade_2_defstat", "scrdtmv_gunner_3", "scrdtmv_spsword_2_deftrig", "scrdtit_purification_rod", "scr_trinket_internal_data_ring", "scrdtenc_darkhall", "sfxset_player_spsword", "scrdtanim_enemy_frog", "scrdtqu_trueend", "scrdtmv_assassin_2", "scrdtpjb_blade", "scrdtpjr_ray", "scrdtmv_spsword_3_defstat", "scrdtmv_shadow_3_defstat", "scrdtit_bloodflower_brooch", "scr_readsheet_anim_simple", "scr_readsheet_encounter", "scrdt_quest_extraimage", "scrdtit_beach_sandals", "scrdten_sanct", "scrdtmv_wizard_1", "scrdten_wolves", "scrdtmv_assassin_3_deftrig", "scrdtit_divine_mirror", "scr_item_internal_data_dlc", "scrdtmv_spsword_3", "scrdtit_throwing_dagger", "scrdtmv_hammer_1_defstat", "scrdtmv_ancient_3_defstat", "scrdtit_grandmaster_spear", "scrdtmv_pyro_3_defstat", "scrdtanim_enemy_spawn", "scrdtit_miners_headlamp", "scrdtmv_dancer_1_deftrig", "scrdtmv_ancient_1_deftrig", "scrdtit_iron_grieves", "scrdtit_stoneplate_armor", "scrdtenc_lakeside", "scrdtit_sketchbook", "scrdtit_canary_charm", "scrdtit_obsidian_hairpin", "sfxdt_pitch", "scrdt_quest_nonstep", "scrit_data_stat", "scrdtmv_gunner_2", "scrdtit_crown_of_storms", "sfxset_player_assassin", "scrdtmv_sniper_1", "scrdtit_shrinemaidens_kosode", "scrhbsex_read_trigger", "scrdtqu_ghost", "scrdtmv_ancient_2_deftrig", "scrdtmv_bruiser_1_deftrig", "scrdtit_ballroom_gown", "scrdtit_oni_staff", "scrhbs_data_set", "scrdtmv_sniper_3", "scrdtit_thiefs_coat", "scrdtit_lightning_bow", "scr_animations_internal_data", "trigger_set", "scrdtmv_pyro_0_defstat", "scrdtpk_upgrade", "scrdtmv_bruiser_2", "scrdtit_crowfeather_hairpin", "scrdtit_shadow_bracelet", "scrdtit_shockwave_tome", "scrdthbs_strike", "scrdten_birds", "scrdtmv_wizard_2", "scrdtmv_shadow_0_deftrig", "scral_data_stat", "scrdtanim_enemy_bird", "scrdtqu_tassha", "scrdtmv_bruiser_0_deftrig", "scrdtit_ornamental_bell", "scrdtit_butterfly_ocarina", "scrdtit_cursed_candlestaff", "scritex_set_weaptype", "scrdtit_timewarp_wand", "scrdtit_tiny_wings", "scrdtpjb_fire2", "scrdthbs_birds", "scrdtqu_other", "scrdtqu_sanct", "scrdtit_artist_smock", "scrdten_aurum", "scrdtmv_hblade_1_defstat", "scrdtmv_gunner_0_deftrig", "scrdtit_crescentmoon_dagger", "scrdtit_blood_vial", "scrdt_quest_step", "scrit_data_trig", "scrdtit_painters_beret", "scrdtit_moss_shield", "scrdthbs_poison", "scritex_set_hbdisptype", "scrdtmv_hammer_1", "scrdtit_giant_stone_club", "scrdthbs_smite", "scrdtit_darkcrystal_rose", "scrdtmv_defender_0_defstat", "scrdtit_talon_charm", "scrdtenc_toybox", "scrext_trig_line", "scrdtmv_hblade_3", "scrit_elmsw", "scrdtit_diamond_shield", "scrdtit_abyss_artifact", "scr_readsheet_populate_possible_encounters", "scrdtmv_dancer_1", "scrdtmv_defender_3", "scrdtmv_shadow_3", "scr_bookofcheats_triggers", "scrtr_data_elm", "sfxset_fire", "scrhbsex_set_floatingtexttype", "scren_data_set", "scrdtmv_gunner_0_defstat", "scrdtenc_arsenal", "scr_readsheet_populate_enumkeys", "scrdtmv_dancer_0", "scrdtit_curse_talon", "scrdtit_stuffed_rabbit", "scrdtit_clockwork_tome", "scrdtit_golden_katana", "scrdtit_spiderbite_bow", "scrdtit_lost_pendant", "sfxset_dlc", "scrdthbs_counter", "scranim_data_set_external_color", "scrdtit_dark_wings", "scrdtit_night_sword", "scrdtmv_gunner_1_defstat", "scrdtmv_hblade_1", "scrdtmv_shadow_0_defstat", "scr_get_list_from_string", "scrhbs_data_set_external", "scrdtit_pidgeon_bow", "scr_get_color_from_hexstring", "scrdtqu_cat", "scrdtmv_shadow_2_deftrig", "scrdtit_trick_shield", "scrdtmv_spsword_2", "scrdtmv_shadow_2", "scrdtit_moon_pendant", "screnc_data_elm", "scr_readsheet_hbs", "scrhbsex_trig_arr", "scrdtit_pointed_ring", "scrdtmv_assassin_0_deftrig", "scrdtit_eternity_flute", "scrdtit_kunoichi_hood", "scrdtit_pocketwatch", "scrdtit_battery_shield", "sfxset_player_hammer", "scrdtqu_battle", "scrdtit_angels_halo", "scrdtit_bloody_bandage", "scrdtanim_enemy_mouse", "trigger_qpatt", "scrdtmv_ancient_2", "scrdtmv_hammer_0", "scrdtmv_pyro_2", "scrdtit_assassins_knife", "scrdtit_ravens_dagger", "scrdtit_old_bonnet", "scrdtit_mountain_staff", "sfxset_generic", "scrdt_quest_clearcount", "scrdtmv_pyro_2_deftrig", "scrdtit_palette_shield", "scrdtit_topaz_charm", "scrdtit_fairy_spear", "scrdtenc_depths", "scrdthbs_decay", "scrdtanim_npc", "scrdtit_large_anchor", "scrdten_frogs", "scrdtmv_none_1", "scrdtit_gemini_necklace", "scrtr_data_set", "scrdtpjb_invis", "scrdtqu_mouse", "scr_readsheet_sheetlist", "scrdtit_redblack_ribbon", "scrdtit_tough_gauntlet", "scrdtit_tidal_greatsword", "scrdthbs_mice", "scrdthbs_fieldlimit", "scrdtit_iron_pickaxe", "scrdtit_strawberry_cake", "scrdtmv_hblade_3_deftrig", "scrdtit_dragonhead_spear", "scritex_set_treasuretype", "instance_create", "scrdtit_blackbolt_ribbon", "scrdtpcn_cone", "scrdthbs_dlc_enemy", "scrdtit_stirring_spoon", "scrdtit_floral_bow", "scrdtit_quartz_shield", "scrdtit_lapis_sword", "scrdtit_aquamarine_bracelet", "sfxset_fire2", "scrdthbs_player",
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
    // Pattern args stuff
    // private readonly List<string> dumpScripts = [
    //     "tcond_bookofcheats_varcheck","tcond_check_flag","tcond_check_no_flag","tcond_dmg_crit","tcond_dmg_islarge","tcond_dmg_self_defensive","tcond_dmg_self_primary","tcond_dmg_self_secondary","tcond_dmg_self_special","tcond_dmg_self_thishb","tcond_dmg_self_weapon","tcond_dmg_nothbs","tcond_equal","tcond_eval","tcond_false","tcond_hb_auto_pl","tcond_hb_available_cdonly","tcond_hb_available","tcond_hb_check_chargeable0","tcond_hb_check_resettable0","tcond_hb_check_square_var_false","tcond_hb_check_square_var_gte","tcond_hb_check_square_var_lte","tcond_hb_check_square_var","tcond_hb_check_stock_gte","tcond_hb_check_stock_lte","tcond_hb_check_stock","tcond_hb_check_var_range","tcond_hb_check_var","tcond_hb_defensive","tcond_hb_loot","tcond_hb_not_self_origin","tcond_hb_not_self","tcond_hb_primary","tcond_hb_secondary","tcond_hb_self_attack","tcond_hb_self_origin","tcond_hb_self_weapon","tcond_hb_self","tcond_hb_selfcast","tcond_hb_special","tcond_hb_team","tcond_hb_type_weapon","tcond_hbs_aflplayer_alive","tcond_hbs_aflplayer_attack","tcond_hbs_aflplayer","tcond_hbs_firststack","tcond_hbs_not_thishbcast","tcond_hbs_self","tcond_hbs_selfafl","tcond_hbs_selfcast","tcond_hbs_target_count","tcond_hbs_thishbcast","tcond_missing_health","tcond_none","tcond_pl_alive","tcond_pl_autocheck","tcond_pl_countcheck","tcond_pl_not_shielded","tcond_pl_self","tcond_player_target_count","tcond_random_def","tcond_random","tcond_shadowemerald_varcheck","tcond_slot_target_count","tcond_square_self","tcond_team_ally","tcond_team_enemy","tcond_tick_every","tcond_trinket_counter_equal","tcond_trinket_counter_greaterequal","tcond_true","tcond_unequal","ttrg_hbstatus_all","ttrg_hbstatus_ally_afflict","ttrg_hbstatus_ally","ttrg_hbstatus_opponent_afflict","ttrg_hbstatus_opponent","ttrg_hbstatus_prune","ttrg_hbstatus_self","ttrg_hbstatus_source","ttrg_hbstatus_target_afflict","ttrg_hbstatus_target","ttrg_hotbarslot_self","ttrg_hotbarslot_source","ttrg_hotbarslots_all","ttrg_hotbarslots_ally","ttrg_hotbarslots_current_players","ttrg_hotbarslots_longestrunningcd","ttrg_hotbarslots_opponent","ttrg_hotbarslots_prune_base_has_str","ttrg_hotbarslots_prune_bool","ttrg_hotbarslots_prune_bufftype","ttrg_hotbarslots_prune_cdstat","ttrg_hotbarslots_prune_cdtype","ttrg_hotbarslots_prune_dealsdamage","ttrg_hotbarslots_prune_hitboxstat","ttrg_hotbarslots_prune_itemstat","ttrg_hotbarslots_prune_itemtype","ttrg_hotbarslots_prune_noreset","ttrg_hotbarslots_prune_self","ttrg_hotbarslots_prune","ttrg_hotbarslots_self_abilities","ttrg_hotbarslots_self_higheststrweapon","ttrg_hotbarslots_self_longestcd","ttrg_hotbarslots_self_loot","ttrg_hotbarslots_self_weapontype_withstr","ttrg_hotbarslots_self_weapontype","ttrg_hotbarslots_target_rand_from_players","ttrg_none","ttrg_player_afflicted_source","ttrg_player_afflicted","ttrg_player_damaged","ttrg_player_prune_hbsflag","ttrg_player_self","ttrg_players_all","ttrg_players_ally_exclude","ttrg_players_ally_include_ko","ttrg_players_ally_lowest_hp","ttrg_players_ally_random","ttrg_players_ally","ttrg_players_none","ttrg_players_opponent_backstab","ttrg_players_opponent_binary","ttrg_players_prune_closest_pos","ttrg_players_prune_closest","ttrg_players_opponent_exclude","ttrg_players_opponent_focus","ttrg_players_opponent_oob","ttrg_players_opponent_random","ttrg_players_opponent","ttrg_players_prune_self","ttrg_players_prune","ttrg_players_source","ttrg_players_target_random","ttrg_players_team_binary","ttrg_trinket_self","tset_animation","tset_colorpalette","tset_critratio","tset_damage_flags","tset_debug_itemkey","tset_debug","tset_hbs_burnhit","tset_hbs_def","tset_hbs_dupevar","tset_hbs_id","tset_hbs_randombuff_damage","tset_hbs_randombuff","tset_hbs_randomdebuff_by_ind","tset_hbs_randomdebuff_key","tset_hbs_randomdebuff_settime","tset_hbs_randomdebuff","tset_hbskey","tset_hbsstr","tset_hbsvars","tset_print","tset_randomness","tset_strength_chargecount","tset_strength_def","tset_strength_loot","tset_strength","tset_strmult_backstab","tset_strmult_debuffcount","tset_transform_id","tset_transform_key","tset_uservar_aflplayer_pos_source","tset_uservar_aflplayer_pos","tset_uservar_battletime","tset_uservar_blackhole_charm_calc","tset_uservar_bound","tset_uservar_cond_squarevar_equal","tset_uservar_darkflame","tset_uservar_difficulty","tset_uservar_each_target_player","tset_uservar_eval","tset_uservar_gold","tset_uservar_hallwaycount","tset_uservar_hb_cooldownvar","tset_uservar_hb_hitboxvar","tset_uservar_hb_itemvar","tset_uservar_hb_stat","tset_uservar_hbscount","tset_uservar_player_stat","tset_uservar_playercount","tset_uservar_random_def","tset_uservar_random_range_int_synced","tset_uservar_random_range_int","tset_uservar_random_range","tset_uservar_random","tset_uservar_slotcount","tset_uservar_spent_slots","tset_uservar_sqvar","tset_uservar_stage","tset_uservar_strength","tset_uservar_switch","tset_uservar","tpat_bookofcheats_set_random","tpat_debug_targets","tpat_hb_add_cooldown_permanent","tpat_hb_add_cooldown_var","tpat_hb_add_cooldown","tpat_hb_add_flag","tpat_hb_add_gcd_permanent","tpat_hb_add_hitbox_var","tpat_hb_add_statchange_norefresh","tpat_hb_add_statchange","tpat_hb_add_strcalcbuff_cooldown","tpat_hb_add_strcalcbuff","tpat_hb_add_strength_hbs","tpat_hb_add_strength","tpat_hb_cdloot_proc","tpat_hb_charge_clear","tpat_hb_charge","tpat_hb_flash_item_source","tpat_hb_flash_item","tpat_hb_hbuse_proc","tpat_hb_inc_var","tpat_hb_increase_stock","tpat_hb_lucky_proc_source","tpat_hb_lucky_proc","tpat_hb_mult_gcd_permanent","tpat_hb_mult_hitbox_var","tpat_hb_mult_length_hbs","tpat_hb_mult_strength_hbs","tpat_hb_mult_strength","tpat_hb_recalc_color","tpat_hb_reduce_stock","tpat_hb_reset_cooldown","tpat_hb_reset_statchange_norefresh","tpat_hb_reset_statchange","tpat_hb_run_cooldown_ext","tpat_hb_run_cooldown_hidden","tpat_hb_run_cooldown","tpat_hb_set_color_def","tpat_hb_set_cooldown_permanent","tpat_hb_set_cooldown_var","tpat_hb_set_gcd_permanent","tpat_hb_set_hitbox_var","tpat_hb_set_stock","tpat_hb_set_strength_cd","tpat_hb_set_strength_darkglass_spear","tpat_hb_set_strength_gcd","tpat_hb_set_strength_obsidian_rod","tpat_hb_set_strength_timespace_dagger","tpat_hb_set_strength","tpat_hb_set_tidalgreatsword_start","tpat_hb_set_tidalgreatsword","tpat_hb_set_var_random_range","tpat_hb_set_var","tpat_hb_square_add_flags","tpat_hb_square_add_var","tpat_hb_square_set_flags","tpat_hb_square_set_var","tpat_hb_transform_item","tpat_hb_zero_stock","tpat_hbs_add_hbsflag","tpat_hbs_add_shineflag","tpat_hbs_add_statchange_bleed","tpat_hbs_add_statchange_sap","tpat_hbs_add_statchange","tpat_hbs_add_var","tpat_hbs_destroy","tpat_hbs_mult_str","tpat_hbs_reset_statchange","tpat_hbs_set_sprite","tpat_hbs_set_var","tpat_nothing","tpat_player_add_gold","tpat_player_add_hp","tpat_player_add_level","tpat_player_add_radius","tpat_player_add_stat","tpat_player_change_color_rand","tpat_player_distcounter_reset","tpat_player_flash_item_transform","tpat_player_movelock","tpat_player_movemult","tpat_player_run_gcd","tpat_player_set_blurcolor","tpat_player_set_fadecolor","tpat_player_set_gold","tpat_player_set_hp","tpat_player_set_level","tpat_player_set_radius","tpat_player_set_stat","tpat_player_shield_hbs","tpat_player_shield","tpat_player_trinket_counter_add_bounded","tpat_player_trinket_counter_add","tpat_player_trinket_counter_randomize","tpat_player_trinket_counter_set_floatingfish","tpat_player_trinket_counter_set","tpat_player_trinket_flash","tpat_player_trinket_update_netplay","tpat_shadowemerald_set_random","tpat_trk_add_hbsflag","tpat_trk_add_shineflag","tpat_trk_add_statchange","tpat_trk_reset_statchange_norefresh","tpat_trk_reset_statchange","hpat_bleed","hpat_burn","hpat_curse","hpat_explosion","hpat_paint","hpat_poison","hpat_snare","hpat_spark","ipat_ancient_0_petonly","ipat_ancient_0_pt2","ipat_ancient_0_rabbitonly","ipat_ancient_0","ipat_ancient_1_auto","ipat_ancient_1_pt2","ipat_ancient_1","ipat_ancient_2_auto","ipat_ancient_2_pt2","ipat_ancient_2","ipat_ancient_3_emerald_pt2","ipat_ancient_3_emerald_pt3","ipat_ancient_3_emerald","ipat_ancient_3","ipat_apply_hbs_paintproc","ipat_apply_hbs_starflash","ipat_apply_hbs","ipat_apply_invuln","ipat_assassin_0_ruby","ipat_assassin_0","ipat_assassin_1_garnet","ipat_assassin_1_ruby","ipat_assassin_1_sapphire","ipat_assassin_1","ipat_assassin_2_opal","ipat_assassin_2","ipat_assassin_3_opal","ipat_assassin_3_ruby","ipat_assassin_3","ipat_black_wakizashi","ipat_blackhole_charm","ipat_blue_rose","ipat_bomb_throw","ipat_bruiser_0_saph","ipat_bruiser_0","ipat_bruiser_1","ipat_bruiser_2","ipat_bruiser_3_emerald","ipat_bruiser_3_pt2","ipat_bruiser_3_ruby","ipat_bruiser_3","ipat_bubble_buff","ipat_butterfly_summon_ring","ipat_butterfly_summon","ipat_butterly_ocarina","ipat_chakram","ipat_claw_slash_hbs","ipat_claw_slash","ipat_crown_of_storms","ipat_curse_talon","ipat_dancer_0_opal","ipat_dancer_0","ipat_dancer_1_emerald","ipat_dancer_1","ipat_dancer_2_saph","ipat_dancer_2","ipat_dancer_3_emerald","ipat_dancer_3","ipat_dark_shield","ipat_darkmagic_blade","ipat_defender_0_fast","ipat_defender_0_ruby","ipat_defender_0","ipat_defender_1_emerald","ipat_defender_1_opal","ipat_defender_1_saph","ipat_defender_1","ipat_defender_2_emerald","ipat_defender_2","ipat_defender_3_pt2","ipat_defender_3","ipat_divine_mirror","ipat_druid_0_emerald","ipat_druid_0_ruby","ipat_druid_0_saph","ipat_druid_0","ipat_druid_1_emerald","ipat_druid_1_garnet","ipat_druid_1_ruby","ipat_druid_1","ipat_druid_2_2_garnet","ipat_druid_2_2","ipat_druid_2_garnet","ipat_druid_2_ruby","ipat_druid_2","ipat_druid_3_emerald","ipat_druid_3_opal","ipat_druid_3_ruby","ipat_druid_3_saph","ipat_druid_3","ipat_erase_area_hbs","ipat_explosion_player","ipat_explosion_spark","ipat_explosion","ipat_flame_kick_miss","ipat_flame_kick_pt2","ipat_flame_kick","ipat_floral_bow","ipat_garnet_staff","ipat_gunner_0_emerald_miss","ipat_gunner_0_emerald_pt2","ipat_gunner_0_emerald","ipat_gunner_0_garnet","ipat_gunner_0_opal","ipat_gunner_0_ruby","ipat_gunner_0_saph","ipat_gunner_0","ipat_gunner_1","ipat_gunner_2_emerald","ipat_gunner_2_garnet","ipat_gunner_2_ruby","ipat_gunner_2_saph","ipat_gunner_2","ipat_gunner_3","ipat_hammer_0_garnet","ipat_hammer_0_saph","ipat_hammer_0","ipat_hammer_1_pt2","ipat_hammer_1_ruby","ipat_hammer_1_saph","ipat_hammer_1","ipat_hammer_2_saph","ipat_hammer_2","ipat_hammer_3_emerald","ipat_hammer_3_pt2","ipat_hammer_3_ruby","ipat_hammer_3","ipat_hblade_0_garnet_pt2","ipat_hblade_0_garnet","ipat_hblade_0","ipat_hblade_1_garnet","ipat_hblade_1_ruby","ipat_hblade_1_saph","ipat_hblade_1","ipat_hblade_2_emerald","ipat_hblade_2_pt2","ipat_hblade_2","ipat_hblade_3_garnet","ipat_hblade_3_opal","ipat_hblade_3_ruby","ipat_hblade_3","ipat_heal_light_maxhealth","ipat_heal_light","ipat_heal_revive","ipat_hydrous_blob","ipat_light_erase","ipat_light_shield_hbs","ipat_light_shield","ipat_lullaby_harp","ipat_magic_hit","ipat_melee_hit","ipat_meteor_staff","ipat_moon_pendant","ipat_nightstar_grimoire","ipat_none_0","ipat_none_1","ipat_none_2","ipat_none_3","ipat_ornamental_bell","ipat_phoenix_charm","ipat_poisonfrog_charm","ipat_potion_throw","ipat_pulse_damage","ipat_pyro_0_emerald","ipat_pyro_0","ipat_pyro_1_garnet","ipat_pyro_1_ruby","ipat_pyro_1","ipat_pyro_2_garnet","ipat_pyro_2_miss_garnet","ipat_pyro_2_miss","ipat_pyro_2_pt2_garnet","ipat_pyro_2_pt2","ipat_pyro_2_saph","ipat_pyro_2","ipat_pyro_3","ipat_reaper_cloak","ipat_red_tanzaku","ipat_shadow_0_emerald","ipat_shadow_0_saph","ipat_shadow_0","ipat_shadow_1_garnet","ipat_shadow_1_pt2_ruby","ipat_shadow_1_pt2","ipat_shadow_1_ruby","ipat_shadow_1_saph","ipat_shadow_1","ipat_shadow_2_emerald","ipat_shadow_2","ipat_shadow_3_emerald","ipat_shadow_3_garnet","ipat_shadow_3_opal","ipat_shadow_3_ruby","ipat_shadow_3","ipat_sleeping_greatbow","ipat_sniper_0_emerald","ipat_sniper_0_garnet","ipat_sniper_0_saph","ipat_sniper_0","ipat_sniper_1_ruby","ipat_sniper_1_saph","ipat_sniper_1","ipat_sniper_2_emerald","ipat_sniper_2_ruby","ipat_sniper_2","ipat_sniper_3","ipat_sparrow_feather","ipat_spsword_0_pt2","ipat_spsword_0","ipat_spsword_1_emerald","ipat_spsword_1_pt2","ipat_spsword_1_ruby","ipat_spsword_1","ipat_spsword_2_pt2","ipat_spsword_2","ipat_spsword_3_pt2","ipat_spsword_3_ruby_pt2","ipat_spsword_3_ruby","ipat_spsword_3","ipat_starflash_failure","ipat_starflash_hbs","ipat_starflash","ipat_stepswing","ipat_swordswing","ipat_thiefs_coat","ipat_timewarp_wand","ipat_topaz_charm","ipat_winged_cap","ipat_wizard_0_ruby","ipat_wizard_0","ipat_wizard_1_garnet_pt2","ipat_wizard_1_garnet","ipat_wizard_1_opal","ipat_wizard_1_ruby","ipat_wizard_1","ipat_wizard_2_ruby","ipat_wizard_2_saph","ipat_wizard_2","ipat_wizard_3_emerald","ipat_wizard_3_opal","ipat_wizard_3",
    //     "scr_pattern_deal_damage_enemy_subtract", "math_lerp_between", "scr_unlockcheck_trigger_called", "scr_triggerex_add_pattern", "math_wave_between", "scrbp_time", "scrbp_pattern_set_projectile_key", "scr_unlockcheck_start", "scr_sound_get_default_volume", "scr_hbbuff_add_enemyanim", "math_percent_between", "scr_damagedisplay_largethreshold", "game_time", "math_lerp_between_unbound", "scr_triggercontroller_unlock_check", "scr_triggercontroller_call_all", "scr_statchanger_reset", "scr_pattern_deal_damage_enemy", "scr_triggerglobals_init_team", "scr_client_has_local_player", "scr_hbbuff_add_damage", "scrbp_add_hbs", "ipat_hammer_0_garnet", "scr_triggerex_reset", "scr_triggerrecp_on_trigger_called", "scr_pattern_set_player", "scr_stagefirst_toybox", "scrbp_pattern_set_color", "scr_triggerglobals_reset", "scr_stagefirst_credits", "math_loop_between", "scr_hbstatus_set_data", "pattern_duration", "scr_hbbuff_cancel_message", "scr_play_sound_pos", "scr_pattern_deal_damage_ally", "scr_pattern_calc_strength", "scr_pattern_set_trigex_vars", "scr_player_invuln_calc", "scr_effect_play_layer", "scr_charanim_reset_stack", "ttrg_hbstatus_self", "scr_obswitch", "scr_pattern_deal_damage", "tparam", "scr_statussys_delete_status", "scrbp_deal_damage_pos", "ttrg_trinket_self", "scr_hbbuff_add_shield", "scr_should_update", "scr_pattern_set_hb", "scrbp_animation", "scr_char_get_hit_enemy", "scr_triggerex_init", "scr_statussys_check_appliable", "scr_statchanger_init", "scr_client_plbin_check", "scr_pattern_check_netpatt", "scr_statussys_add_status", "scr_udpcont_send_now", "scr_char_play_animation_ext", "scr_damagedisplay_add", "scrbp_time_repeat_times", "scr_hbsflag_check", "scr_char_change_animation_base", "scr_triggerrecp_update_params", "scr_statcontroller_refresh", "scr_timegt_set", "scr_trigger_call", "scrbp_init_common_variables", "scr_triggerex_add_pattern_quick", "scr_effect_play", "scr_charanim_calculate_frame", "scr_player_invuln", "scrbp_effect_charge", "sbsv", "scr_pattern_reset_quick", "scr_char_get_hit_ally", "scr_statussys_find_status_enemydebuff", "sbgv", "scr_effect_play_battle", "math_bound", "scr_damagedisplay_add_message", "object_get_depth", "scr_view_camera_shake", "scr_triggerrecp_add_trigger_id", "scr_pattern_set_rollback", "scr_pattnetbuff_create_buffer", "scr_client_pllocal_disp", "scr_statusys_status_applied", "scr_play_sound_pos2", "scr_triggerrecp_run_instructions", "scr_pattern_set_strcalc", "scrbp_movespeed_mult", "scr_char_set_motion", "ttrg_hotbarslot_self", "scrbp_pattern_set_drawlayer", "scr_pattern_update_quick", "scr_statussys_find_status", "scrbp_time_repeating", "scrbp_time_repeat_until", "scrbp_effect_attack", "scr_effect_play_parent", "scr_paindisplay_hit", "instance_create", "scrbp_sound", "scr_char_set_colorflash", "scr_char_get_shielded", "scr_char_play_animation_pattern", "scrbp_deal_damage", "scr_triggerex_execute", "scr_unlockcont_trigger_handling", "scr_frame_stop", "scr_input_vibrate_input"
    // ];
    private List<String> dumpFuncs = new List<string> {"random_set_seed","ds_list_copy","ds_list_shuffle","ds_list_create","ds_list_size","ds_list_find_value","ds_list_delete","ds_list_add","array_shuffle","array_shuffle_ext","random","random_range","irandom","irandom_range","choose","randomize", "ds_map_clear", "ds_map_create", "merge_color", "ds_map_destroy", "array_get", "is_undefined", "ds_map_find_value", "ds_map_set", "ini_read_string"};
    // Only log these scripts if used inside other (logged) scripts
    private List<String> badScripts = new List<string> {
        "scr_difficulty", "scr_stagefirst_toybox", "math_fit_within", "scr_lang_string", "scr_stagefist_available", "scr_diffswitch", "scr_get_gx", "scr_get_gy", "scr_lang_get", "math_bound", "scr_lang_set_font_ext", "scr_lang_get_font_ext", "scr_obswitch", "scr_loadsprite_get", "scr_loadsprite_is_loaded", "scr_lang_fw_switch_ext", "scr_input_check_nonlocal", "scr_stagefirst_available", "scr_client_plbin_check", "scr_client_has_local_player", "scr_statcontroller_refresh", "scr_lang_get_scale_ext", "scr_triggerglobals_reset", "scr_timegt_reset", "scr_timegt_update", "scr_lang_fw_switch", "math_lerp_between", "scr_lang_fe_switch", "math_percent_between", "math_diff", "scr_timect_reset", "scr_timect_update", "math_lerp_smooth_gt", "scr_lang_set_font", "scr_timect_check", "scr_lang_get_font", "scr_char_change_animation_base", "scr_input_check", "rgb", "math_lerp_between", "scr_should_update", "math_loop_between", "math_lerp_between_unbound", "math_wave_between", "scr_client_plbin_check", "scr_client_pllocal_disp", "scr_stagefirst_credits", "game_time", "scr_hbsflag_check", "scr_charanim_calculate_frame",
        "scr_pattern_deal_damage_enemy_subtract", "math_lerp_between", "scr_unlockcheck_trigger_called", "scr_triggerex_add_pattern", "math_wave_between", "scrbp_time", "scrbp_pattern_set_projectile_key", "scr_unlockcheck_start", "scr_sound_get_default_volume", "scr_hbbuff_add_enemyanim", "math_percent_between", "scr_damagedisplay_largethreshold", "game_time", "math_lerp_between_unbound", "scr_triggercontroller_unlock_check", "scr_triggercontroller_call_all", "scr_statchanger_reset", "scr_pattern_deal_damage_enemy", "scr_triggerglobals_init_team", "scr_client_has_local_player", "scr_hbbuff_add_damage", "scrbp_add_hbs", "ipat_hammer_0_garnet", "scr_triggerex_reset", "scr_triggerrecp_on_trigger_called", "scr_pattern_set_player", "scr_stagefirst_toybox", "scrbp_pattern_set_color", "scr_triggerglobals_reset", "scr_stagefirst_credits", "math_loop_between", "scr_hbstatus_set_data", "pattern_duration", "scr_hbbuff_cancel_message", "scr_play_sound_pos", "scr_pattern_deal_damage_ally", "scr_pattern_calc_strength", "scr_pattern_set_trigex_vars", "scr_player_invuln_calc", "scr_effect_play_layer", "scr_charanim_reset_stack", "ttrg_hbstatus_self", "scr_obswitch", "scr_pattern_deal_damage", "tparam", "scr_statussys_delete_status", "scrbp_deal_damage_pos", "ttrg_trinket_self", "scr_hbbuff_add_shield", "scr_should_update", "scr_pattern_set_hb", "scrbp_animation", "scr_char_get_hit_enemy", "scr_triggerex_init", "scr_statussys_check_appliable", "scr_statchanger_init", "scr_client_plbin_check", "scr_pattern_check_netpatt", "scr_statussys_add_status", "scr_udpcont_send_now", "scr_char_play_animation_ext", "scr_damagedisplay_add", "scrbp_time_repeat_times", "scr_hbsflag_check", "scr_char_change_animation_base", "scr_triggerrecp_update_params", "scr_statcontroller_refresh", "scr_timegt_set", "scr_trigger_call", "scrbp_init_common_variables", "scr_triggerex_add_pattern_quick", "scr_effect_play", "scr_charanim_calculate_frame", "scr_player_invuln", "scrbp_effect_charge", "sbsv", "scr_pattern_reset_quick", "scr_char_get_hit_ally", "scr_statussys_find_status_enemydebuff", "sbgv", "scr_effect_play_battle", "math_bound", "scr_damagedisplay_add_message", "object_get_depth", "scr_view_camera_shake", "scr_triggerrecp_add_trigger_id", "scr_pattern_set_rollback", "scr_pattnetbuff_create_buffer", "scr_client_pllocal_disp", "scr_statusys_status_applied", "scr_play_sound_pos2", "scr_triggerrecp_run_instructions", "scr_pattern_set_strcalc", "scrbp_movespeed_mult", "scr_char_set_motion", "ttrg_hotbarslot_self", "scrbp_pattern_set_drawlayer", "scr_pattern_update_quick", "scr_statussys_find_status", "scrbp_time_repeating", "scrbp_time_repeat_until", "scrbp_effect_attack", "scr_effect_play_parent", "scr_paindisplay_hit", "instance_create", "scrbp_sound", "scr_char_set_colorflash", "scr_char_get_shielded", "scr_char_play_animation_pattern", "scrbp_deal_damage", "scr_triggerex_execute", "scr_unlockcont_trigger_handling", "scr_frame_stop", "scr_input_vibrate_input",
    };
    private string dumpFilename = "dump/definitions.txt";
    private bool isInsideScr = false;
    private bool shouldDumpFuncs = false; // set by config

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

            this.shouldDumpFuncs = this.config.DumpFuncsConfig;
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

            // var script = rnsReloaded.GetScriptData(rnsReloaded.ScriptFindId("ipat_hblade_0") - 100000);
            // this.hookMap["ipat_hblade_0"] = hooks.CreateHook<ScriptDelegate>(this.hbladePrim, script->Functions->Function)!;
            // this.hookMap["ipat_hblade_0"].Activate();
            // this.hookMap["ipat_hblade_0"].Disable();

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
            if (this.hookMap.ContainsKey(scriptName)) {
                this.logger.PrintMessage($"Script has already been hooked for logging {scriptName}!", this.logger.ColorRed);
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
        } else if (val->Type == RValueType.Ref) {
            ret = Regex.Replace(ret, @"^ref [a-z]+ ", "");
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
        } else if (name == "scrtr_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (trinket)argv[i]->Int64;
                args.Add($"{nameof(trinket)}.{member}");
                if (member == trinket.color) {
                    args.Add(Rgb64ToHex((long)argv[i+1]->Real));
                } else if (member == trinket.flag1 || member == trinket.flag2) {
                    args.Add("0b" + Convert.ToString(RValueToLong(argv[i+1]), 2));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "screnc_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (encounter)argv[i]->Int64;
                args.Add($"{nameof(encounter)}.{member}");
                if (member == encounter.category) {
                    args.Add($"{nameof(encCategory)}.{(encCategory)argv[i+1]->Int64}");
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scren_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (enemy)argv[i]->Int64;
                args.Add($"{nameof(enemy)}.{member}");
                if (member == enemy.color || member == enemy.colorSaturated) {
                    args.Add(Rgb64ToHex((long)argv[i+1]->Real));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scrnpc_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (npc)argv[i]->Int64;
                args.Add($"{nameof(npc)}.{member}");
                if (member == npc.color) {
                    args.Add(Rgb64ToHex((long)argv[i+1]->Real));
                } else {
                    args.Add(this.GetString(argv[i+1]));
                }
            }
        } else if (name == "scral_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (ally)argv[i]->Int64;
                args.Add($"{nameof(ally)}.{member}");
                args.Add(this.GetString(argv[i+1]));
            }
        } else if (name == "scral_data_stat") {
            for (var i = 0; i < argc; i += 2) {
                var member = (stat)argv[i]->Int64;
                args.Add($"{nameof(stat)}.{member}");
                args.Add(this.GetString(argv[i+1]));
            }
        } else if (name == "scral_data_color") {
            if (argc < 8) {
                this.logger.PrintMessage("scral_data_color has fewer args than expected", Color.Red);
                for (var i = 0; i < argc; i += 1) {
                    args.Add(this.GetString(argv[i]));
                }
            } else {
                args.Add(this.GetString(argv[0]));
                args.Add(this.GetString(argv[1]));
                args.Add(Rgb64ToHex((long)argv[2]->Real));
                args.Add(Rgb64ToHex((long)argv[3]->Real));
                args.Add(Rgb64ToHex((long)argv[4]->Real));
                args.Add(Rgb64ToHex((long)argv[5]->Real));
                args.Add(Rgb64ToHex((long)argv[6]->Real));
                args.Add(Rgb64ToHex((long)argv[7]->Real));
            }
        } else if (name == "scranim_data_elm") {
            for (var i = 0; i < argc; i += 2) {
                var member = (anim)RValueToLong(argv[i]);
                args.Add($"{nameof(anim)}.{member}");
                args.Add(this.GetString(argv[i+1]));
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
            if (script_name.Contains("ttrg_hotbarslots") && argc > 1) {
                var arg1_is_int64 = argv[1]->Type == RValueType.Int64;
                if (script_name.EndsWith("prune")) {
                    var (str1, str2) = this.ParseParams(argv[1], argv[3]);
                    args.Add(str1);
                    args.Add(this.GetString(argv[2]));
                    args.Add(str2);
                    next_index = 4;
                } else if ((script_name.EndsWith("cdtype") || script_name.EndsWith("cdstat")) && arg1_is_int64) {
                    var member = (cdType)argv[1]->Int64;
                    args.Add($"{nameof(cdType)}.{member}");
                    next_index = 2;
                } else if (script_name.EndsWith("hitboxstat") && arg1_is_int64) {
                    var member = (hitbox)argv[1]->Int64;
                    args.Add($"{nameof(hitbox)}.{member}");
                    next_index = 2;
                } else if (script_name.EndsWith("itemstat") && arg1_is_int64) {
                    var member = (item)argv[1]->Int64;
                    args.Add($"{nameof(item)}.{member}");
                    next_index = 2;
                } else if (script_name.Contains("weapontype") && arg1_is_int64) {
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

    private static long RValueToLong(RValue* val) {
        if (val->Type == RValueType.Real) {
            return (long)val->Real;
        } else if (val->Type == RValueType.Int32) {
            return val->Int32;
        } else if (val->Type == RValueType.Int64) {
            return val->Int64;
        } else {
            Console.WriteLine($"Rvalue was not real or int: {val->ToString()}");
            return 0;
        }
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

            // this.dumpRooms($"dump/rooms{++this.dumpIndex}.json");


            // foreach (var str in new List<string> {
            //                     "arsenal",
            //                     "asha",
            //                     "aura",
            //                     "avy",
            //                     "bloom",
            //                     "butterfly0",
            //                     "butterfly1",
            //                     "butterfly3",
            //                     "depths",
            //                     "kingdom",
            //                     "lakeside",
            //                     "lighthouse",
            //                     "matti",
            //                     "mell",
            //                     "merran",
            //                     "moon",
            //                     "nest",
            //                     "outskirts",
            //                     "ranalie",
            //                     "rings",
            //                     "sanct",
            //                     "seal",
            //                     "shira",
            //                     // "speaker",
            //                     "spell",
            //                     "sphere",
            //                     "star",
            //                     "streets",
            //                     "twili",
            //                     "witch"
            //                     }) {
            //     var strR = rnsReloaded.utils.CreateString(str)!.Value;
            //     rnsReloaded.ExecuteScript("scr_dialog_handle_flag_color", null, null, [new RValue(0.0), strR]);
            // }
            // Console.WriteLine("Finished dialoging");

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
        if (this.rnsReloadedRef!.TryGetTarget(out var rnsReloaded) && this.dumpIndex == 0) {
            List<string> scripts = ["hpat_bleed","hpat_burn","hpat_curse","hpat_explosion","hpat_paint","hpat_poison","hpat_snare","hpat_spark","ipat_ancient_0_petonly","ipat_ancient_0_pt2","ipat_ancient_0_rabbitonly","ipat_ancient_1_auto","ipat_ancient_1_pt2","ipat_ancient_1","ipat_ancient_2_auto","ipat_ancient_2_pt2","ipat_ancient_2","ipat_ancient_3_emerald_pt2","ipat_ancient_3_emerald_pt3","ipat_ancient_3_emerald","ipat_ancient_3","ipat_apply_hbs_paintproc","ipat_apply_hbs_starflash","ipat_apply_hbs","ipat_apply_invuln","ipat_assassin_0_ruby","ipat_assassin_0","ipat_assassin_1_garnet","ipat_assassin_1_ruby","ipat_assassin_1_sapphire","ipat_assassin_1","ipat_assassin_2_opal","ipat_assassin_2","ipat_assassin_3_opal","ipat_assassin_3_ruby","ipat_assassin_3","ipat_black_wakizashi","ipat_blackhole_charm","ipat_blue_rose","ipat_bomb_throw","ipat_bruiser_0_saph","ipat_bruiser_0","ipat_bruiser_1","ipat_bruiser_2","ipat_bruiser_3_emerald","ipat_bruiser_3_pt2","ipat_bruiser_3_ruby","ipat_bruiser_3","ipat_bubble_buff","ipat_butterfly_summon_ring","ipat_butterfly_summon","ipat_butterly_ocarina","ipat_chakram","ipat_claw_slash_hbs","ipat_claw_slash","ipat_crown_of_storms","ipat_curse_talon","ipat_dancer_0_opal","ipat_dancer_0","ipat_dancer_1_emerald","ipat_dancer_1","ipat_dancer_2_saph","ipat_dancer_2","ipat_dancer_3_emerald","ipat_dancer_3","ipat_dark_shield","ipat_darkmagic_blade","ipat_defender_0_fast","ipat_defender_0_ruby","ipat_defender_0","ipat_defender_1_emerald","ipat_defender_1_opal","ipat_defender_1_saph","ipat_defender_1","ipat_defender_2_emerald","ipat_defender_2","ipat_defender_3_pt2","ipat_defender_3","ipat_divine_mirror","ipat_druid_0_emerald","ipat_druid_0_ruby","ipat_druid_0_saph","ipat_druid_0","ipat_druid_1_emerald","ipat_druid_1_garnet","ipat_druid_1_ruby","ipat_druid_1","ipat_druid_2_2_garnet","ipat_druid_2_2","ipat_druid_2_garnet","ipat_druid_2_ruby","ipat_druid_2","ipat_druid_3_emerald","ipat_druid_3_opal","ipat_druid_3_ruby","ipat_druid_3_saph","ipat_druid_3","ipat_erase_area_hbs","ipat_explosion_player","ipat_explosion_spark","ipat_explosion","ipat_flame_kick_miss","ipat_flame_kick_pt2","ipat_flame_kick","ipat_floral_bow","ipat_garnet_staff","ipat_gunner_0_emerald_miss","ipat_gunner_0_emerald_pt2","ipat_gunner_0_emerald","ipat_gunner_0_garnet","ipat_gunner_0_opal","ipat_gunner_0_ruby","ipat_gunner_0_saph","ipat_gunner_0","ipat_gunner_1","ipat_gunner_2_emerald","ipat_gunner_2_garnet","ipat_gunner_2_ruby","ipat_gunner_2_saph","ipat_gunner_2","ipat_gunner_3","ipat_hammer_0_saph","ipat_hammer_0","ipat_hammer_1_pt2","ipat_hammer_1_ruby","ipat_hammer_1_saph","ipat_hammer_1","ipat_hammer_2_saph","ipat_hammer_2","ipat_hammer_3_emerald","ipat_hammer_3_pt2","ipat_hammer_3_ruby","ipat_hammer_3","ipat_hblade_0_garnet_pt2","ipat_hblade_0_garnet","ipat_hblade_1_garnet","ipat_hblade_1_ruby","ipat_hblade_1_saph","ipat_hblade_1","ipat_hblade_2_emerald","ipat_hblade_2_pt2","ipat_hblade_2","ipat_hblade_3_garnet","ipat_hblade_3_opal","ipat_hblade_3_ruby","ipat_hblade_3","ipat_heal_light_maxhealth","ipat_heal_light","ipat_heal_revive","ipat_hydrous_blob","ipat_light_erase","ipat_light_shield_hbs","ipat_light_shield","ipat_lullaby_harp","ipat_magic_hit","ipat_melee_hit","ipat_meteor_staff","ipat_moon_pendant","ipat_nightstar_grimoire","ipat_none_0","ipat_none_1","ipat_none_2","ipat_none_3","ipat_ornamental_bell","ipat_phoenix_charm","ipat_poisonfrog_charm","ipat_potion_throw","ipat_pulse_damage","ipat_pyro_0_emerald","ipat_pyro_0","ipat_pyro_1_garnet","ipat_pyro_1_ruby","ipat_pyro_1","ipat_pyro_2_garnet","ipat_pyro_2_miss_garnet","ipat_pyro_2_miss","ipat_pyro_2_pt2_garnet","ipat_pyro_2_pt2","ipat_pyro_2_saph","ipat_pyro_2","ipat_pyro_3","ipat_reaper_cloak","ipat_red_tanzaku","ipat_shadow_0_emerald","ipat_shadow_0_saph","ipat_shadow_0","ipat_shadow_1_garnet","ipat_shadow_1_pt2_ruby","ipat_shadow_1_pt2","ipat_shadow_1_ruby","ipat_shadow_1_saph","ipat_shadow_1","ipat_shadow_2_emerald","ipat_shadow_2","ipat_shadow_3_emerald","ipat_shadow_3_garnet","ipat_shadow_3_opal","ipat_shadow_3_ruby","ipat_shadow_3","ipat_sleeping_greatbow","ipat_sniper_0_emerald","ipat_sniper_0_garnet","ipat_sniper_0_saph","ipat_sniper_0","ipat_sniper_1_ruby","ipat_sniper_1_saph","ipat_sniper_1","ipat_sniper_2_emerald","ipat_sniper_2_ruby","ipat_sniper_2","ipat_sniper_3","ipat_sparrow_feather","ipat_spsword_0_pt2","ipat_spsword_0","ipat_spsword_1_emerald","ipat_spsword_1_pt2","ipat_spsword_1_ruby","ipat_spsword_1","ipat_spsword_2_pt2","ipat_spsword_2","ipat_spsword_3_pt2","ipat_spsword_3_ruby_pt2","ipat_spsword_3_ruby","ipat_spsword_3","ipat_starflash_failure","ipat_starflash_hbs","ipat_starflash","ipat_stepswing","ipat_swordswing","ipat_thiefs_coat","ipat_timewarp_wand","ipat_topaz_charm","ipat_winged_cap","ipat_wizard_0_ruby","ipat_wizard_0","ipat_wizard_1_garnet_pt2","ipat_wizard_1_garnet","ipat_wizard_1_opal","ipat_wizard_1_ruby","ipat_wizard_1","ipat_wizard_2_ruby","ipat_wizard_2_saph","ipat_wizard_2","ipat_wizard_3_emerald","ipat_wizard_3_opal","ipat_wizard_3"];
            // List<string> scripts = [
            // ];
            foreach (var script in scripts) {
                rnsReloaded.ExecuteScript(script, self, other, []);                
            }
        }
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
