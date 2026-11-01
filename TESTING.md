# In-game test checklist

Not tested in game yet. Setup: a gravship in orbit, CE setting "Enable CIWS" on, dev mode on, at least one CIWS
turret with an anti-projectile mode (e.g. PB MIDAS, PB Skyranger35, VFE Security point defense) loaded with ammo.
Start events from the dev menu: incidents `VGE_EscapePodCrash`, `VGE_AsteroidShower`, `VGE_SpaceDebris` (or the
matching game conditions).

## Startup

- [ ] No errors from `[VGECEPatch]` or about the patched defs in the log (config errors for the six VGE defs,
      "uses legacy collision code", "lerpPosition", "not ProjectilePropertiesCE").
- [ ] Info card of `VGE_MediumAsteroid` shows CE projectile stats.

## Regression: ordinary drop pods (bug found 2026-10-26)

- [ ] CIWS turrets shoot at enemy drop pods (raid by drop pods from the dev menu), as they do without this mod.
- [ ] They still shoot at CE mortar shells.

## VFE Pirates gauntlet raid (VFE Pirates active)

- [ ] "Manage CIWS targets" lists the four gauntlet ships ("ship (incoming)"), ticked.
- [ ] Dev menu: raid by pirates with strategy gauntlet. CIWS turrets fire at the falling ships, and it takes about
      220 damage (10 times a drop pod) to bring one down; a drop pod still goes down at about 22.
- [ ] An enemy shuttle (e.g. an Empire or VFE Empire shuttle raid) takes about 66.
- [ ] No "[VGECEPatch] ... has no CE interception pool to scale" warning at startup.
- [ ] A ship shot down: raiders fall out dead (corpses near where it was hit), salvage (steel, components, slag)
      at its landing spot, no crash explosion or crashed-ship turret; no errors.
- [ ] A ship that survives lands as in VFE Pirates, its raiders wounded by the hits it took.
- [ ] A friendly gauntlet raid (allied pirates/junkers/mercenaries) is not shot at.

## Escape pod

- [ ] The turret's "Manage CIWS targets" list includes "escape pod (incoming)", ticked (targeted).
- [ ] With the pod in range the turret fires at it, and about 22 damage brings it down.
- [ ] Shot down: no roof hole, no destroyed building, no blast mark, no hackable pod left; no errors.
- [ ] Untick it in the list: the pod lands as in VGE (roof hole, building on the cell destroyed, hackable pod).
- [ ] Out of range or CIWS hold-fire: lands as in VGE.

## Asteroid shower and space debris

- [ ] Asteroids and debris appear at the map edge and cross the map at the same speed as before; asteroids spin,
      no shadow; debris comes from one edge in a near-parallel stream.
- [ ] One hitting a wall explodes there (CE explosion); a standing pawn and a DOWNED pawn in the path are hit.
- [ ] One leaving the map drops nothing.
- [ ] Impact loot: asteroids leave a vacstone chunk (medium/large sometimes a forbidden resource stack); large
      debris sometimes steel slag; small/medium debris nothing.
- [ ] "Manage CIWS targets" lists all six (small/medium/large asteroid, small/medium/large debris), ticked.
- [ ] The turret engages them: a shot-down one disappears without exploding and drops its impact loot where it was.
      Roughly 5 damage brings one down.
- [ ] Rounds per kill: noticeably fewer than before the swept hit check, most of all on large asteroids and
      small fast debris. No CE config error about CompProperties_CIWSTarget at startup.
- [ ] Walls: a turret with a wall between it and the intercept point does not fire at that asteroid.
- [ ] Pawns near a passing asteroid are not suppressed.
- [ ] VGE's own gravship point-defense turret still shoots asteroids and debris down (smoke puff, the projectile
      vanishes without exploding, loot drops), with more misses on fast debris, as in VGE.
- [ ] Unticking them in the list stops the turret firing at them.
- [ ] No log spam while a turret searches for targets during a shower (CE's launcher null check is patched).
- [ ] CIWS rounds that miss: note where they land (they can hit things near the intercept point, as CE misses do).

## Chapter 2 compact turrets (Chapter 2 active)

- [ ] No `PatchOperationMakeGunCECompatible ... doesn't exist` warning and no config errors for the six
      `VGE_*Gravship*Turret` / `VGE_Gun_*Gravship*` defs.
- [ ] Gravship minigun, autocannon and uranium slug turrets: info card shows CE stats (range 56 / 71 / 77, minimum
      range only on the uranium slug, 5), an ammo gizmo, and no steel/uranium fuel gizmo.
- [ ] Architect menu cost and work: minigun 175 steel, 40 gravlite, 5 components, work 50000; autocannon 213 steel,
      40 gravlite, 5 components, work 35000; uranium slug 233 steel, 30 gravlite, 60 uranium, 5 components, work 35000.
- [ ] Hit points 285 / 375 / 375; power 200 / 500 / 500; magazines 250 / 200 / 20.
- [ ] Built on substructure, a colonist hauls 7.62x51mm / 20x102mm / 40x311mmR ammo and reloads it; it fires bursts
      of 16 / 7 / 1 with VGE's sounds, about 1.25 / 1.0 / 1.5 s apart.
- [ ] EMP still stuns them; they don't explode when destroyed.
- [ ] Enemy gravship versions fire CE rounds and run dry after one magazine.
- [ ] With Chapter 2 disabled the mod loads without errors (the ModPatches folder is skipped).

## Gravship armor (Chapter 2 active)
- [ ] Raid with mortars against a gravship walled with gravship armor: shells whose path crosses the armor
      explode on it; the interior is untouched by those blasts.
- [ ] Shells that never cross the armor land normally; your own mortars fire out over your own armor.
- [ ] Shelling an enemy ship that has armor: your shells stop on its armor.
- [ ] Shields still stop shells first; CIWS still shoots shells down before they reach the armor.

## Anticraft caster and emitter
- [ ] Info card of the caster beam shows armor penetration 1.5 and damage 10 (emitter 2.55 and 32, Chapter 2).
- [ ] A caster burst now wounds a pawn in power armor (about 6 damage per hit); the emitter about 25 per hit.
- [ ] Centipedes take about 8 (caster) / 29 (emitter) per hit.

## Combat vacsuit (Chapter 2 active)
- [ ] Info card: suit sharp 5.92, blunt 11.76, bulk 80, worn bulk 15, covers hands and feet.
- [ ] Info card: helmet sharp 5.92, blunt 12.25, bulk 4, worn bulk 1, with weaker armor listed for eyes, nose and jaw.
- [ ] No XML errors at startup.

## Save / load

- [ ] Save and load mid-shower with asteroids in flight: they keep flying straight and still collide.
- [ ] Save and load with an escape pod falling: it is still targeted.
