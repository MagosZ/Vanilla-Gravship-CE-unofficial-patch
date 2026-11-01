# Vanilla Gravship CE Patch (Unofficial): design

Status: design FINAL (agreed 2026-10-22). Built 2026-10-22 (compiles, both XML patches dry-run on VGE 1.6/1.7
defs); not tested in game yet, see TESTING.md. Section 3 (Chapter 2 turrets) added and built the same day.

Goal: Combat Extended point-defense (CIWS) turrets can shoot down two Vanilla Gravship Expanded threats that
they currently ignore: the escape pod crash and the asteroid shower (plus its twin, the space debris event).

## 0. Basics

- Name: "Vanilla Gravship CE Patch (Unofficial)", author MagosZ. packageId and Harmony id `you.vgecepatch.unofficial`
  (kept, as ElevatedGunPlatform kept its own). GitHub: MagosZ/Vanilla-Gravship-CE-unofficial-patch, CC BY-NC-SA 4.0.
- Requires Harmony, Combat Extended, Vanilla Gravship Expanded - Chapter 1. Loads after CE and both VGE chapters.
- Game 1.6 and 1.7. One net48 DLL built against Krafs.Rimworld.Ref 1.6 + CE's DLL + VGE's DLL (both not copied),
  Lib.Harmony 2.4.3 with runtime excluded; same setup as ElevatedGunPlatform.
- Source references: VGE Ch1 `Vanilla-Expanded/VanillaGravshipExpanded` @7212a0c, CE master @0e01c62.

## 1. Escape pod crash

VGE's `IncidentWorker_EscapePodCrash` drops `VGE_EscapePodSkyfaller` (class `Skyfaller_EscapePod`) onto a
constructed-roof cell; on impact it breaks the roof and destroys every building on that cell.

Why CE ignores it today: CE's anti-skyfaller verb (`VerbCIWSSkyfaller`) only looks at real transport pods
(`ThingRequestGroup.ActiveTransporter` / `IActiveTransporter`), and `Skyfaller_EscapePod` is a plain `Skyfaller`.
CE would also call it friendly, because its only content is a faction-less building.

Design:
- HP: unchanged. The pod's parent is `DropPodIncoming`, so it already has CE's `CompCIWSImpactHandler_Skyfaller`
  with HP 22 (CE `Patches/Core/Skyfallers/SkyfallerBase.xml`), exactly a drop pod's pool.
- Harmony postfix on `VerbCIWSSkyfaller.Targets` (getter): also return spawned escape pods (every ThingDef whose
  thingClass is `Skyfaller_EscapePod` or a subclass, looked up by def).
- Harmony postfix on `VerbCIWSSkyfaller.IsFriendlyTo`: escape pods are never friendly.
- Harmony postfix on `VerbProperties_CIWSSkyfaller.InitAllTargets`: add the escape pod defs, so they appear in each
  turret's "Manage CIWS targets" window. ON by default (CE targets everything not in the turret's ignored list,
  which starts empty).
- When shot down, CE's handler runs as for any pod: the damaged-pod building inside leaves its leavings, the pod
  vanishes, no impact (no roof hole, nothing destroyed). The hack loot is rolled only when hacked, so nothing else.

## 2. Asteroid shower and space debris

Defs: `VGE_SmallAsteroid_Projectile`, `VGE_MediumAsteroid`, `VGE_LargeAsteroid` (asteroid shower) and
`VGE_SmallDebris`, `VGE_MediumDebris`, `VGE_LargeDebris` (space debris).

VGE today: vanilla projectiles (`Projectile_Space : Projectile_Explosive`, not overhead) spawned at a map edge and
launched flat across the map toward a cell 1000 cells away; they explode on the first wall or pawn in their path.
CE's anti-projectile verb (`VerbCIWSProjectile`) only targets CE projectiles, so it ignores them.

Design: turn them into CE projectiles that fly flat, NOT mortar-class. (`flyOverhead` would switch off all CE
collision along the path, and shields would swallow them silently.)

- XML patch per def: thingClass -> our `ProjectileCE_VGESpace` (subclass of CE `ProjectileCE_Explosive`);
  `projectile` becomes `CombatExtended.ProjectilePropertiesCE`; damageDef (Bomb), damageAmountBase and
  explosionRadius kept; flyOverhead stays false; `suppressionFactor` 0 (a rock does not suppress); our
  straight-line trajectory worker; comp `CompCIWSImpactHandler` with HP 5 (a drop pod has 22; was 11, half a pod's, lowered by the user
  2026-10-26 for balance).
- Speed: CE speed is cells per second, vanilla is speed/100 cells per tick, so CE speed = VGE speed x 0.6 to keep
  the same speed on screen. Asteroids 32/24/18 -> 19.2/14.4/10.8; debris 46/36/26 -> 27.6/21.6/15.6.
  All stay under CE's CIWS speed cap of 80.
- Flight: own `BaseTrajectoryWorker` subclass: a straight line at a constant height of 0.02. CE gravity 0 is not
  usable (flight time becomes infinite and `ticksToImpact` breaks). Flight time = time to cross the map, so path
  collision stays active the whole way. Height 0.02 is above the ground (CE treats height <= 0 as hitting the
  ground) and below a downed human (0.09 = 1.0 x 0.3 x 0.3 in CE's `GetCollisionBodyFactors`), so it hits walls,
  standing and downed pawns and anything else CE counts as solid in its path.
- Collision and explosion: CE rules. CE checks pawns and things with Fillage != None along the path and explodes
  with a CE explosion at the hit (a ground burst at height < 2.0 needs line of sight from the blast, so walls
  shelter what is behind them).
- Spawning: VGE spawns with `(Projectile)GenSpawn.Spawn(...)` and vanilla `Launch`, which fails for a CE projectile.
  Harmony prefixes replace `GameCondition_AsteroidShower.SpawnProjectile` and `GameCondition_SpaceDebris.SpawnProjectile`
  with copies that keep VGE's rules (asteroids: random standable unroofed edge cell, aimed at the map centre +-25
  degrees; debris: the event's saved edge and base angle +-5 degrees) and VGE's own type pickers
  (`GetRandomAsteroidType` / `GetRandomDebrisType`, called by reflection, so phase weights stay VGE's). Then CE
  `Launch` with no launcher, angle 0, VGE's rotation, height 0.02, the CE speed above.
- Loot (moved into our class, same rules as VGE): when the projectile hits something or is shot down, it drops at
  its position: asteroids a vacstone chunk, plus 10% (medium) / 25% (large) a resource stack (steel or uranium
  5-15, gold 10-30, plasteel 1-5, forbidden); large debris a 25% steel slag chunk; small and medium debris nothing.
  Leaving the map drops nothing.
- CIWS:
  - Harmony postfixes on `VerbProperties_CIWSProjectile.Interceptable` and `.InitAllTargets`: accept these defs
    (CE requires flyOverhead there). ON by default in every turret's "Manage CIWS targets" list.
  - Harmony prefix on `VerbCIWSProjectile.IsFriendlyTo`: these are never friendly. Also needed because CE reads
    `thing.launcher.HostileTo(...)`, which would throw for VGE's launcher-less projectiles on every target search.
  - Walls block: CE's CIWS aiming has no line-of-sight check (it assumes targets are in the sky). For these
    projectiles only, the shot line must also pass CE's own line-of-sight check from the turret to the predicted
    intercept point. Patch point: the `VerbCIWSProjectile.IsFriendlyTo` prefix above (CE calls it only in its target
    search): it runs CE's own `TryFindCEShootLineFromTo` for the intercept point and treats a blocked line as "skip".
    A target already being fired at is not re-checked mid-burst (CE keeps bursting at a target it has).
    NEVER patch a method declared in the generic `VerbCIWS<T>` (2026-10-26 bug): its compiled code is shared by
    every reference-type T, and Harmony's copy hard-codes the T named in the patch, so patching
    `VerbCIWS<ProjectileCE>.TryFindCEShootLineFromTo` made every skyfaller verb fail its `is TargetType` check and
    CIWS turrets stopped firing at drop pods.
  - Interception: each CIWS round that hits takes its damage off the 5 HP pool; at 0 the projectile is destroyed
    with no explosion and drops its loot where it was.
  - Hit check (user, 2026-10-26, to cut ammo spent per asteroid): CE's default for projectile targets only tests
    the round's end-of-tick position against a fixed 1-cell circle, so fast rounds step over the target and size
    does not matter. `CompCIWSTarget_VGESpace` (CE's own `CompCIWSTarget.CanCollideWith` hook, no Harmony) instead
    takes both this tick's movements and counts a hit at the closest approach if it is within
    drawSize x `radiusFactor` + `radiusBonus` (set in the XML patch, no rebuild needed). Tripled by the user on
    2026-10-28 from 0.5 / 0.5 (1 / 1.5 / 2 cells, still too few hits) to 1.5 / 1.5: small 3, medium 4.5, large 6
    cells. Horizontal only, like CE's default. Its properties class `CompProperties_CIWSTarget_VGESpace` derives
    from plain `CompProperties` because CE's `CompProperties_CIWSTarget` config check rejects any subclass comp
    (inverted `IsAssignableFrom`). Being a `CompCIWSTarget` also lists the asteroids in CE's comp-target list, which only
    `VerbCIWS_CompSkyfaller` reads, for its own comp type, so targeting is unchanged.
- Who can shoot them: every turret with a CE anti-projectile CIWS verb (installed: PB MIDAS, PB MLG27,
  PB Skyranger35, Local's PDC sentry, VFE Security point defense). Normal weapons cannot target them.
- VGE's own gravship point-defense turret (`VGE_PointDefenseTurret`, `CompPointDefence`) keeps intercepting them
  as in VGE (found in the 2026-10-26 recheck: its `t is Projectile_Space` check and vanilla-only hit chance no
  longer matched them). Postfixes on `CompPointDefence.IsValidProjectile` (accept them) and `.InterceptChance`
  (VGE's own formula on VGE's original speed, CE speed / 0.6); a successful intercept drops the loot, as VGE's
  projectiles did whenever they were destroyed.

## 3. Chapter 2 compact turrets (agreed 2026-10-22, rebalanced the same day)

VGE Chapter 2 (`vanillaexpanded.gravship2`) adds three compact 1x1 gravship turrets, built on substructure, plus
enemy copies used on enemy gravships. All six are vanilla `Building_TurretGun`s with vanilla `Verb_Shoot`; the player
ones are refuelled with steel or uranium (`CompRefuelable` + `consumeFuelPerShot`). Neither CE nor any installed mod
patches them, and CE's gun auto-patcher (off by default) skips turret guns.

| Building (player / enemy) | Gun (player / enemy) |
|---|---|
| `VGE_GravshipMinigunTurret` / `VGE_EnemyGravshipMinigunTurret` | `VGE_Gun_GravshipMinigun` / `VGE_Gun_EnemyGravshipMinigun` |
| `VGE_GravshipAutocannonTurret` / `VGE_EnemyGravshipAutocannonTurret` | `VGE_Gun_GravshipAutocannon` / `VGE_Gun_EnemyGravshipAutocannon` |
| `VGE_GravshipUraniumSlugTurret` / `VGE_EnemyGravshipUraniumSlugTurret` | `VGE_Gun_GravshipUraniumSlug` / `VGE_Gun_EnemyGravshipUraniumSlug` |

Loading: `loadFolders.xml` loads `ModPatches/VGE_Chapter2/` only if `vanillaexpanded.gravship2` is active (1.6 and
1.7), so Chapter 2 stays optional. Patch file: `ModPatches/VGE_Chapter2/Patches/VGE2_CompactTurrets.xml`.

### 3.1 The rule

**Every value = CE's value for the matching turret x (VGE value / vanilla value).** Where VGE equals vanilla, CE's
value is taken as is. It applies to everything: combat stats, magazine, hit points, power, cost, build work, minimum
range, accuracy, and the explode-when-destroyed comp (CE removes it, so it goes). The user rejected keeping raw VGE
values for "identity": the first version mixed CE ammo with VGE's raw bursts and cooldowns, leaving the autocannon at
about a quarter of CE's autocannon's firepower.

Pairings:
- Autocannon: vanilla `Turret_Autocannon` / `Gun_AutocannonTurret` and CE's versions of them.
- Uranium slug: vanilla `Turret_Sniper` / `Gun_TurretSniper` (the uranium slug turret) and CE's versions.
- Minigun: no vanilla turret twin. The gun pairs with the hand-held `Gun_Minigun` (same `Bullet_Minigun`); the
  building pairs with `Turret_MiniTurret`, the other 1x1 turret.

Vanilla to VGE (the ratios used):

| | Vanilla autocannon -> VGE | Vanilla uranium slug -> VGE | Minigun (mini-turret building / hand minigun gun) -> VGE |
|---|---|---|---|
| Size, hit points | 2x2, 380 -> 1x1, 285 | 2x2, 380 -> 1x1, 285 | 1x1, 100 -> 1x1, 285 |
| Cost | 350 steel, 40 plasteel, 6 comp -> 175 steel, 40 gravlite, 3 comp | 300 steel, 30 plasteel, 60 uranium, 6 comp -> 150 steel, 30 gravlite, 60 uranium, 3 comp | 70 steel, 3 comp -> 175 steel, 40 gravlite, 3 comp |
| Build work | 15000 -> 15000 | 15000 -> 15000 | 1800 -> 15000 |
| Power | 150 -> 150 | 150 -> 150 | 80 -> 160 |
| Range (min) | 32.9 (8.9) -> 29.9 (8.9) | 45.9 (11.9) -> 40.9 (11.9) | 30.9 -> 27.9 |
| Burst | 3 shots, 17 ticks apart -> 2, 20 apart | 1 -> 1 | 25 shots, 5 ticks apart -> 8, 7 apart |
| Burst cooldown s | 3.5 -> 3.5 | 3.2 -> 4.8 | 4.8 (mini-turret) -> 6 |
| Shots per load | 90 (2 steel/shot) -> 180 (1 steel/shot) | 30 (2 uranium/shot) -> 30 (2 uranium/shot) | not comparable |
| Turret accuracy | 0.96 -> 0.96 | 0.98 -> 0.98 | 0.96 -> 0.96 |
| Explodes when destroyed | radius 5.9 -> 1.9 | radius 5.9 -> 1.9 | radius 3.9 -> 1.9 |

The descriptions' "more efficient" holds only for the autocannon's steel use; the uranium slug uses 2 uranium per
shot in both. In combat the VGE turrets are equal or slightly weaker than vanilla; they are smaller and cheaper.

### 3.2 Results (player and enemy get the same values)

Buildings:

| | Minigun | Autocannon | Uranium slug |
|---|---|---|---|
| Hit points | 285 (CE kept the mini-turret's) | 285 x 500/380 = 375 | 375 |
| Flammability | 0.7 (CE kept the mini-turret's) | 0.4 (CE's) | 0.4 |
| Build work | 15000 x 6000/1800 = 50000 | 35000 (CE's) | 35000 |
| Steel | 175 (CE kept 70) | 425 x 175/350 = 213 | 465 x 150/300 = 233 |
| Gravlite | 40 | 40 (replaces plasteel, which CE left at 40) | 30 |
| Uranium | - | - | 60 |
| Components | 3 x 5/3 = 5 | 9 x 3/6 = 5 (4.5 rounded up) | 10 x 3/6 = 5 |
| Power (player) | 160 x 100/80 = 200 | 500 (CE's) | 500 |
| Burst cooldown s | 1.0 x 6/4.8 = 1.25 | 1.0 | 1.0 x 4.8/3.2 = 1.5 |
| ShootingAccuracyTurret / AimingAccuracy | 0.5 / 0.25 (CE's mini-turret) | 1.25 / 0.75 | 1.5 / 0.75, NightVisionEfficiency 0.5 |
| Explodes when destroyed | removed | removed | removed |

Guns (CE `PatchOperationMakeGunCECompatible`):

| | Minigun | Autocannon | Uranium slug |
|---|---|---|---|
| Ammo set / default round | `AmmoSet_762x51mmNATO` / FMJ | `AmmoSet_20x102mmNATO` / AP | `AmmoSet_40x311mmR` / AP |
| Magazine / reload s | 250 / 9.2 | 100 x 180/90 = 200 / 7.8 | 20 / 6.8 |
| Range | 62 x 27.9/30.9 = 56 | 78 x 29.9/32.9 = 71 | 86 x 40.9/45.9 = 77 |
| Min range | none | none (CE removed vanilla's 8.9) | 5 (CE's) |
| Burst | 50 x 8/25 = 16 shots, 2 x 7/5 = 3 ticks apart | 10 x 2/3 = 7 shots, 7 x 20/17 = 8 ticks apart | 1 |
| Warmup s | 2.1 (CE minigun) | 2.3 | 4.6 |
| SightsEfficiency / ShotSpread / SwayFactor | 1 / 0.06 / 1.61 (mounted) | 1 / 0.01 / 1.61 | 2.36 / 0.01 / 1.89 |
| RangedWeapon_Cooldown / recoil | 0.35 / 0.97 | 0.37 / 1.50 | 0.5 / 4.15 |
| Fire modes | aimed burst 8, AI snapshot | AI aimed shot, no snapshot, no single shot | AI aimed shot, no snapshot |

Sustained fire against CE's own turrets: autocannon about 78% of CE's autocannon (vanilla: VGE 70% of vanilla),
uranium slug 67% (vanilla: 67%).

Also: thingClass `CombatExtended.Building_TurretGunCE` and fillPercent 0.85 on both abstract parents; "Stun" removed
from the stunnable comp (EMP stays), as CE does; player turrets lose `CompProperties_Refuelable`. Kept from VGE: size,
research, substructure requirement, sounds and muzzle flash (recoil pattern Mounted), descriptions. VGE's own
projectile defs (`VGE_Bullet_GravshipAutocannon`, `VGE_Bullet_GravshipUraniumSlug`) become unused and are left alone.

VGE code checked for turret ties: Chapter 1's `Building_TurretGun` hooks only act on `Building_GravshipTurret` and the
point-defense turret; Chapter 2's act on the gauss guns, the wormspitter and orbital-cluster dormancy, none on these
six. So switching them to CE's turret class loses no VGE behaviour.

Enemy ammo (correction to the earlier discussion): CE fills a non-player turret's magazine once when it first spawns
and never refills it on its own (unmanned turrets only reload from an autoloader or a hauling colonist). So enemy
turrets get one full CE magazine, not unlimited ammo; that is CE's normal behaviour for every enemy turret.
DECIDED (user, 2026-10-22): keep CE's default, no extra ammo for enemy turrets.

## 4. VFE Pirates gauntlet raid ships (added 2026-10-26, user request)

Vanilla Factions Expanded - Pirates (`oskarpotocki.vfe.pirates`, optional; source reference
`Model mods/_Mod_Source_Reference/VanillaFactionsExpanded-Pirates` @be4040e). The gauntlet raid (`VFEP_GauntletStrat`, arrival
`VFEP_GauntletDrop`, `PawnsArrivalModeWorker_Gauntlet`) drops one 5x3 gauntlet ship per 8 raiders: skyfaller defs
`VFEP_Ship_Black/Green/Orange/Red` (thingClass `VFEPirates.IncomingSmoker : Skyfaller`, parent `SkyfallerBase`)
carrying a `Building_CrashedShip` (600 HP turret with the raiders in its own container; killed leavings 120 steel,
6 components, 4 steel slag). The `VFEP_IShipChunk*` debris skyfallers use the same class but only appear in the "low
orbit crash" start scenario, never in the raid, so they are not targets.

Why CE ignores the ships: like the escape pod, `IncomingSmoker` is not a transport pod.

HP (user, 2026-10-26): shuttles get 3 times a drop pod's pool (66) and gauntlet ships 10 times a drop pod's
(220; briefly 660, 10 times a shuttle's, lowered for balance). CE itself gives every skyfaller, shuttles included, the one `SkyfallerBase` pool of 22, which children
inherit rather than declare (a second comp in XML would add a second pool). So `Patches/CIWSPools.xml` puts a
`CIWSPoolMultiplier` mod extension on the abstract parents (`ShuttleSkyfallerBase` x3, VFE Pirates' `VFEP_Ship` x10,
the latter only if it exists), every child inherits it, and `Source/CIWSPoolMultiplier.cs` multiplies each such
def's pool once the defs are loaded.

Shuttles covered: every def built on `ShuttleSkyfallerBase`: Royalty's shuttle (incoming, leaving, crashing),
Odyssey's passenger shuttle, and in the user's mods VFE Empire's armored shuttle, VGE Chapter 2's salvager
dropship, VGE Expanded's heavy dropship, Better Traders Guild's guild shuttle, the Trailblazer shuttle, and
Gravship Raids' arriving enemy gravships (built on the shuttle base too). The multiplier only matters where CE can
target the def at all (its anti-skyfaller verb takes transport-pod skyfallers).

Design:
- Targets: the escape pod hooks (section 1) cover a shared list `TargetDefs.ExtraSkyfallers` = escape pods + the four
  ship defs (by name; empty without VFE Pirates). Listed and ON by default in "Manage CIWS targets".
- Friendliness: CE's own check. CE's `ContainedThings` yields the crashed-ship building, which has the raiders'
  faction, so enemy ships are hostile and a friendly gauntlet raid ("dropping in to help you") is not shot.
- Crew (CE's drop pod rule): CE only looks for pawns directly in a pod or in nested pods, not inside the crashed-ship
  building, so without help a hit never wounds the raiders and a shot-down ship deletes them with no corpses.
  `GauntletShipCIWS.cs`: postfix on `CompCIWSImpactHandler_Skyfaller.OnImpact` wounds a random raider per hit; prefix
  on `.OnDestroying` drops every raider under the ship (or at its landing cell if that is off the map) and kills
  them, as CE does for pod pawns. CE's handler then leaves the wreck's killed leavings at the landing spot and
  removes the ship: no crash explosion, no fires, no turret.
- CE predicts skyfaller positions with its own model (current angle, no zPositionCurve); its hit test uses the same
  model, so interception is consistent, but the drawn ship may be slightly off the point where it is hit.

## 5. Chapter 2 gravship armor (added 2026-10-28, user request)

VGE Chapter 2's `VGE_GravshipArmor` (a `Wall` child, 2000 HP) needs nothing from CE as a wall: fillPercent 1,
impassable, no damage multipliers (CE's Wall patch removes them; vanilla 1.6 has none), CE's bullet building factor
applies by damage type. Its feature is VGE2's `Projectile_Launch_Patch`: a vanilla projectile whose path crosses armor
owned by a faction hostile to the shooter is retargeted onto that armor cell, overhead shells included. That patch is
on vanilla `Projectile.Launch`, so it still covers VGE's own vanilla artillery under CE but never CE projectiles.

`Source/Patches/GravshipArmorInterception.cs` gives CE projectiles the same rule:
- Postfix on `ProjectileCE.CheckForCollisionBetween` (called every tick after the move, before the ground check),
  only when CE found no collision itself (shields and physical hits come first). It walks the cells from `LastPos`
  to `ExactPosition` and takes the first armor whose faction is hostile to the launcher's faction. Height is
  ignored, as in VGE.
- Caught: the projectile is placed on the armor cell centre at height 1 (half CE's wall height 2), `landed`, and
  `Impact(armor)`. Below wall height, CE's explosion only reaches the side the shell came from (`GenExplosionCE`
  passes the shell's direction; an explosion centred on a wall needs line of sight to the open neighbour on that
  side), so the blast stays outside the hull. At or above wall height CE would hit every unroofed cell in range.
- Skipped: CE CIWS rounds (`ProjectileCE_CIWS` overrides the method without calling base) and projectiles with no
  launcher faction (asteroids; a shell whose shooter was destroyed before a save, which CE nulls on save).
- Not decided by "fired by a colonist" (user's first idea): enemy gravships carry this armor too, so the owner's
  hostility decides, as in VGE, and player shells hit enemy armor.

## 6. Anticraft caster and emitter penetration (added 2026-10-28, user request)

CE leaves both turrets as VGE's vanilla energy weapons (its gun autopatcher skips VGE's verb and beam classes), so
their beams reach pawns through CE's armor code with VGE's vanilla penetration. Heat damage under CE:
damage x (pen - armor) / pen per layer, 0 when armor >= pen (vanilla instead rolls deflect / half on armor - pen).
At pen 0.5 the caster did nothing to power armor (heat 0.54, kept by CE) or recon armor (0.46).

Penetration tripled (user, 2026-10-28; doubled first, then raised) and damage per shot doubled (user, same day:
centipedes, CE heat armor 0.25, still soaked the hits up), only when Combat Extended is loaded (`PatchOperationFindMod`, name "Combat Extended"):
caster `VGE_Bullet_AnticraftCaster` pen 0.5 -> 1.5, damage 5 -> 10 (`Patches/VGE_Anticraft.xml`; also Chapter 2's enemy anticraft
caster, which shares the bullet), emitter `VGE_Bullet_AnticraftEmitter` pen 0.85 -> 2.55, damage 16 -> 32
(`ModPatches/VGE_Chapter2/Patches/VGE2_Anticraft.xml`). Per hit on the outer layer: caster 6.4 vs power armor, 6.9 vs recon, 8.3 vs a centipede;
emitter 25.2 vs power, 26.2 vs recon, 28.9 vs a centipede (VGE's values gave 0 / ~0.4 and 5.8 / 7.3). Damage to
buildings and ships is unchanged (CE's armor code is pawns only). The enemy anticraft beam
(`VGE_Bullet_AnticraftBeam`, its burning strike) is a different weapon and is left alone.

## 7. Chapter 2 combat vacsuit (added 2026-11-01, user request)

CE patches Odyssey's plain vacsuit and VGE Chapter 1's prestige vacsuit (sharp 3.5, blunt 7, bulk 80 / 12, hands and
feet covered; helmet bulk 4 / 1, smoke sensitivity -1, partial armor) but has no Chapter 2 folder, so
`VGE_Apparel_CombatVacsuit` and its helmet kept vanilla-scale armor (0.88 sharp, 0.42 blunt) and no bulk.

`ModPatches/VGE_Chapter2/Patches/VGE2_CombatVacsuit.xml`, only when Combat Extended is loaded. Armor is CE's plain
vacsuit value x (combat / plain, vanilla values); the user chose this over scaling from power armor:

| | Sharp | Blunt | Bulk / worn |
|---|---|---|---|
| Suit | 3.5 x 0.88/0.52 = 5.92 | 7 x 0.42/0.25 = 11.76 | 80 / 18 (plain 80 / 12; worn set by the user) |
| Helmet | 3.5 x 0.88/0.52 = 5.92 | 7 x 0.42/0.24 = 12.25 | 4 / 1 (as plain) |

Copied from CE's plain vacsuit: hands and feet covered; helmet smoke sensitivity -1, eyes at 0.70 and nose and jaw at
0.50 of its armor. Heat (0.78), mass, speed and vacuum resistance stay VGE's.

## 8. Known differences and risks

- Explosions follow CE rules instead of vanilla.
- Shields: CE only lets a shield stop a projectile whose launcher is hostile (unless the shield intercepts
  non-hostile projectiles), so with no launcher shields will not stop them. Vanilla behaviour is believed to be the
  same; unverified.
- Asteroids already in flight when the mod is added to a save may break; don't add it mid-shower.
- The two spawn methods are copies of VGE's code; if VGE changes its spawn rules, re-sync them.
- Compact turrets: the VGE descriptions still mention refuelling with steel or uranium and exploding when damaged;
  existing turrets in a save lose their refuelled steel/uranium and start with an empty CE magazine (player) when
  the mod is added. Cost changes don't refund or charge turrets already built.
- Not tested in game until built; needs a test checklist (TESTING.md) like ElevatedGunPlatform.
