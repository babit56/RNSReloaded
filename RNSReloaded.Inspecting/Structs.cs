namespace RNSReloaded.Inspecting.Structs;

enum treasureType {
  none,
  all,
  generic,
  purple,
  blue,
  red,
  yellow,
  green,
  purpleblue,
  purplered,
  purpleyellow,
  purplegreen,
  bluered,
  blueyellow,
  bluegreen,
  redyellow,
  redgreen,
  yellowgreen,
  glacier,
  memory,
  cultist,
  painters,
  daynight,
  sharpedge,
  oceans,
  performers,
  miners,
  teaparty,
  regenPotion,
  potion,
  upgradeP,
  upgradeS,
  upgradeSp,
  upgradeD,
}

enum trgType {
  none,
  cdCalc0,
  strCalc0,
  cdCalc1,
  cdCalc2a,
  cdCalc2b,
  cdCalc3,
  cdCalc4a,
  cdCalc4b,
  cdCalc5,
  cdCalc6,
  cdCalc7, // quickdraw - guessed name
  strCalc1a,
  strCalc1b,
  strCalc1c,
  strCalc2,
  strCalc3,
  strCalc4,
  strCalc5,
  strCalc6,
  finalCalc,
  colorCalc,
  colorCalc2,
  adventureStart,
  hallwayStart,
  battleStart0,
  battleStart2,
  battleStart3,
  battleEnd0,
  battleEnd1,
  battleEnd2,
  battleEnd3,
  battleEnd4,
  hbsCreated,
  hbsCreatedSelf,
  hbsRefreshed,
  hbsDestroyed,
  hbsFlagTrigger,
  hbsShield0,
  hbsShield1,
  hbsShield2,
  hbsShield3,
  hbsShield4,
  hbsShield5,
  hotbarUsed,
  hotbarUsedProc,
  hotbarUsed2,
  hotbarUsedProc2,
  hotbarUsed3,
  hotbarUsedProc3, // only springloaded scythe
  hotbarUsedProcDelayed, // used by sin o2, hblade g1
  onDamage,
  onHealed,
  onInvuln,
  onDamageDone,
  onHealDone,
  onEraseDone,
  regenTick,
  distanceTick,
  standingStill,
  distanceTickBattle,
  standingStillBattle,
  luckyProc,
  cdLootProc,
  charProc, // used by gunner defs, for special onhit effects
  paintProc,
  autoStart,
  autoEnd,
  onSpecialCond0,
  onSpecialCond1,
  onSquarePickup0, // like half of the pickup stuff ish
  onSquarePickup1, // only rainbow cape
  onSquarePickup2, // the other half of pickup stuff
  onSquarePickup, // unused, deprecated
  onGoldChange,
  onAbilityUpgrade, // Sets random debuff for fight on shadow e3 and gunner o2
  onItemRefresh, // pyrite earrings
  onLevelup,
  onDefeatEnemy, // used for sanctum music unlock. See also EnemyDefeatReason enum
  enrageStart, // red tanzaku
  patternSpecial, // See e.g. mice holy shield
}

enum stat {
  none,
  hp, //Adjusts Max HP
  primaryMult, //Makes your Primary deal extra damage (as a percentage)
  secondaryMult, //Above, but for Secondary
  specialMult, //Above, but for Special
  defensiveMult, //Above, but for Defensive
  lootMult, //Above, but for Loot items
  allMult, //Will make ALL damage you deal greater by a percentage
  hbsMult, //Will make Status Effects deal you place deal more damage
  primaryMultHbs, //For the following variables, same as above, but they are added in later in calculations.  These are only meant to be on Status Effects like Flash-Int, and should probably not be used for items.
  secondaryMultHbs,
  specialMultHbs,
  defensiveMultHbs,
  lootMultHbs,
  allMultHbs,
  hbsMultHbs,
  damageMult, //This makes afflicted characters TAKE more damage by a percentage.  It is used for things like Curse, and shouldn't be placed on items.
  damagePlusP0, //These make afflicted characters TAKE more damage by a flat number.  Used for Bleed's effect, and shouldn't be placed on items.
  damagePlusP1,
  damagePlusP2,
  damagePlusP3,
  cdp, //Adds a flat value to all cooldowns this character has, measured in milliseconds.  Currently only used on Firescale Corset.
  haste, //Increases or decreases GCDs by a percentage
  luck, // Makes the character luckier by a percentage // confirmed (luck potion)
  critDamage, // add to crit (see crit items)
  primaryCritRatio, // always crit primary (springloaded scythe)
  secondaryCritRatio,
  specialCritRatio,
  defensiveCritRatio, // unused
  lootCritRatio,
  allCritRatio, // unused
  startingGold, //Makes your character START with more gold.  This is only used in toybox mode to make Silver Coin work there.  It won't affect anything mid-run.
  charspeed, //Makes your character move faster or slower. // confirmed (reaper cloak)
  radius, //Makes your character's hitbox larger or smaller.  Used on Sunflower Crown and Evasion Potion, which have -10 each
  invulnPlus, //Make invulnerability effects last longer (or shorter) by a flat amount, in milliseconds.
  stockPlus, //Currently does nothing
  hbsFlag, //"Special flags that affect your character in various ways. This is a binary number, so multiple values can be combined to have multiple effects."
  hbShineFlag, //"Makes abilities on the mini-hotbar shine, indicating that they're stronger.  Used on status effects like Flash-Int, Flow-Str or Super. Can also be used to cross them out and make them unusable. This is a binary number, so multiple values can be combined to have multiple effects."
}

enum statChangerCalc {
  addPercent,
  addFlat,
  multiply,
  binary,
}

enum cdType {
  none, // Nothing
  time, // Just a cooldown like most Defensives
  gcd, // GCD + maybe a cooldown, like Primary or Special
  stock, // Just a cooldown with multiple uses, uses restore with a cooldown like Wizard Defensive
  stockGcd, // Has multiple uses and a GCD, like Heavyblade Special
  stockOnly, // Only has Stock, cooldown does not restore uses, like Defender Special
}

enum lootHbType {
  none, // Doesn't show a small item near your mini hotbars
  cooldown, // Shows a small cooldown for the item (Ex: Spiderbite Bow, Holy Greatsword)
  cooldownVarAm, // Shows a small cooldown for the item, but does not show if the item's sqVar0 is 0 (Currently only used for Sapphire Violin)
  varAm, // Will show the sqVar0 of the item (Ex: Tornado Staff, Marble Clasp, Staticshock Earrings)
  glowing, // Item will glow if the item's sqVar0 is over 0 (Ex: Demon Horns, Ruby Circlet)
  glowing1, // maybe red/green outline?
  presist, // robe of light/dark
  varWeaponType, // shows item with ability symbol?, e.g. whitewing bracelet
}

enum hitboxInput {
  none,
  press,
  hold,
  auto,
}

enum cdResetType {
  normal,
  reset,
  stockPlus,
  quiet, // maybe unused?
}

enum weaponType {
  none,
  primary,
  secondary,
  special,
  defensive,
  loot,
  potion,
}

enum itemType {
  character,
  weapon,
  loot,
  potion,
  pickup,
  upgrade,
  any,
  none,
}

enum chargeTypes {
  none,
  charge,
  supercharge,
  ultracharge,
  omegacharge,
  darkspell,
  shackles, // Used by shadow on all abilities
}

enum elements {
  none,
  white,
  purple,
  blue,
  red,
  yellow,
  green,
}

enum item {
  key,
  type,
  name,
  desc,
  sprite,
  subimg,
  evoIcon,
  element,
  flags,
  flagSp0,
  flagSp1,
  showSqVar,
  lootHbDispType,
  greySqVar0,
  glowSqVar0,
  autoOffSqVar0,
  color,
  color2, // color of other item in day/night set
  treasureType,
  transformKey, // day/night set
  transformId,
}

enum hitbox {
  radius,
  delay,
  hbInput,
  weaponType,
  strMult,
  number,
  critRatio, // Only used by sharpedge shield
  hbsType,
  hbsStrMult,
  hbsLength,
  chargeType,
  luck,
  condFunc0,
  condFunc1,
  color0,
  color1,
  flags,
  var0,
  var1,
  var2,
  var3,
}

enum hotbarStatus {
  // 0
  // 1
  name = 2,
  desc,
  sprite,
  subimg,
  color,
  // 7 - unused
  isVisible = 8,
  isBuff,
  isPermanent,
  isDefense,
  isDamaging, // only used by burns
  deleteOnKo, // only used by "hbs_targeted_alive"
  displayNumber, // bool, always 0?
  displayTimer,
  refreshType, // enum, mostly 3 for buffs and 1 for debuffs
  targetType, // smite
  floatingTextType, // 2 for decay/freeze, 0 for new wolf vanish and lonely debuffs
  stackMax, // decay freeze
  paletteDependant,
}

enum hbsRefreshType {
  overwrite,
  noOverwrite,
  shorttime,
  longtime,
  stackShorttime,
  stackLongtime,
  multipleInst,
}

enum hbsTargetType {
  both,
  ally,
  enemy,
}

enum cdInfo {
  cooldownType,
  cooldown,
  gcdLength,
  hiddenCooldown,
  stockDecrease,
  stockIncrease,
  maxStock,
}

enum hbFlashMessage {
  none,
  reset,
  proc,
  plus,
  failed,
  shield,
  broken,
  transformed = 11,
}

enum difficulty {
  cute,
  normal,
  hard,
  lunar,
}

enum stage {
  test,
  outskirts, //Kingdom Outskirts
  nest, //Scholar's Nest
  arsenal, //King's Arsenal
  lighthouse, //Red Darkhouse
  streets, //Churchmouse Streets
  lakeside, //Emerald Lakeside
  keep, //Pale Keep
  pinnacle, //Moonlit Pinnacle
}

enum goldSource {
  battleRewards, //Gold gained from battle rewards
  store, //Gold lost from buying things in a store
  loot, //Gold gained from an item effect
  debug, //Gold gained via dev mode commands
}

// See screnc_data_elm - guessed names - TODO
enum encounter {
  name = 2,
  hitbox,
}

enum hbsFloatingTextType {
  none,
  standard,
  stack,
}