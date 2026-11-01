# Vanilla Gravship CE Patch (Unofficial)

[![License: CC BY-NC-SA 4.0](https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg)](https://creativecommons.org/licenses/by-nc-sa/4.0/)

An unofficial Combat Extended patch for Vanilla Gravship Expanded, for RimWorld 1.6 and 1.7. CE's point-defense
(CIWS) turrets can shoot down the VGE threats they used to ignore, and several VGE weapons and defences are adjusted
so they work under CE.

The design spec is in `DESIGN.md`. The in-game test checklist is in `TESTING.md`.

## What it changes

CIWS must be enabled in the CE settings. Every new CIWS target below is on by default in each turret's
"Manage CIWS targets" list.

**CIWS interception pools.** A CIWS turret brings a target down once its hits add up to the target's interception
hit points:

| Target | Interception hit points |
|---|---|
| Asteroid or space debris, any size | 5 |
| Drop pod, escape pod | 22 (CE's own value) |
| Shuttle (any shuttle-based skyfaller, other mods' included) | 66 |
| VFE Pirates gauntlet ship | 220 |

**Escape pod crash.** The incoming escape pod is a CIWS target, like an enemy drop pod. Shot down, it breaks up in
the air: no hole in the roof, nothing destroyed below, and whatever was inside is gone.

**Asteroid shower and space debris.**
- Asteroids and debris become CE projectiles. They fly flat across the map at VGE's speed, very low (CE height
  0.02), and explode on the first wall or pawn in their path, downed pawns included.
- Turrets with CE's anti-projectile CIWS mode target them, but only with line of sight to the point where they will
  meet. A round hits if it passes within 3, 4.5 or 6 cells of a small, medium or large rock at any point of its
  flight. The radius is set in `Patches/VGE_SpaceProjectiles.xml` (`radiusFactor`, `radiusBonus`).
- Shot down, a rock breaks up without exploding and drops VGE's impact loot where it was.
- VGE's own point-defense gun still shoots them down with VGE's odds.
- They don't suppress pawns.

**VFE Pirates gauntlet raid** (VFE Pirates active). Incoming gauntlet ships are CIWS targets; a friendly raid's are
left alone. As with CE drop pods, each hit wounds a raider inside. A ship shot down drops its raiders dead and
leaves its wreck's salvage, with no crash explosion.

**Chapter 2 compact turrets** (Chapter 2 active). The gravship minigun, autocannon and uranium slug turrets, and the
enemy versions, become CE turrets with CE ammo, magazines and reloading. Every stat is CE's value for the matching
turret, scaled by how the VGE turret compares with its vanilla version.

| Turret | Ammo | Magazine | Range | Burst |
|---|---|---|---|---|
| Gravship minigun | 7.62x51mm NATO | 250 | 56 | 16 |
| Gravship autocannon | 20x102mm | 200 | 71 | 7 |
| Gravship uranium slug | 40x311mmR | 20 | 77 (minimum 5) | 1 |

Enemy turrets start with one full magazine, as every CE enemy turret does.

**Gravship armor** (Chapter 2 active). VGE makes hostile fire that crosses gravship armor hit the armor, but only
for vanilla projectiles. This patch extends that to CE projectiles, mortar shells included: a projectile fired by
a faction hostile to the armor's owner explodes on the first armor it crosses, and the blast stays outside the hull.

**Anticraft caster and emitter.** Under CE's heat armor rule their beams barely hurt armored pawns or mechanoids, so
with Combat Extended loaded:

| Beam | Armor penetration | Damage per shot |
|---|---|---|
| Anticraft caster (also the enemy caster) | 0.5 → 1.5 | 5 → 10 |
| Anticraft emitter (Chapter 2) | 0.85 → 2.55 | 16 → 32 |

**Combat vacsuit** (Chapter 2 active). CE patches the plain and prestige vacsuits but not this one, so with Combat
Extended loaded it gets armor scaled from CE's plain vacsuit by the vanilla ratio, plus bulk. Hands and feet are
covered, and the helmet gets CE's smoke protection and weaker armor on the eyes, nose and jaw, as on the plain vacsuit.

| | Sharp | Blunt | Bulk / worn bulk |
|---|---|---|---|
| Combat vacsuit | 0.88 → 5.92 | 0.42 → 11.76 | 80 / 18 |
| Combat vacsuit helmet | 0.88 → 5.92 | 0.42 → 12.25 | 4 / 1 |

## Requirements and load order

- Harmony, Combat Extended, Vanilla Gravship Expanded - Chapter 1.
- Optional: Vanilla Gravship Expanded - Chapter 2, Vanilla Factions Expanded - Pirates.
- Load after all of them.

## Known limitations

- Don't add the mod while an asteroid shower or debris event is under way; rocks already in flight may break.
- Asteroids and debris have no shooter, so shields that only stop hostile fire don't stop them.
- The asteroid and debris spawn methods follow VGE's code; if VGE changes its spawn rules they need re-syncing.
- Compact turrets: VGE's descriptions still mention refuelling and exploding. Turrets already built in a save lose
  their refuelled steel or uranium and start with an empty CE magazine; cost changes don't refund or charge them.
- A CE shell whose shooter was destroyed before a save has no shooter after loading, so gravship armor lets it pass.

## Building from source

Requirements:
- A .NET SDK that can build net48. On Windows this needs the .NET Framework 4.8 targeting pack.
- The NuGet packages restore by themselves: Krafs.Rimworld.Ref 1.6.4871 and Lib.Harmony 2.4.3, which is
  compile-only.
- CombatExtended.dll and VanillaGravshipExpanded.dll, referenced from `$(CEDir)\Assemblies` and
  `$(VGEDir)\1.6\Assemblies`. Set `CEDir` and `VGEDir` in `Source/VGECEPatch.csproj` to your mod folders, or pass
  `-p:CEDir=...` and `-p:VGEDir=...` on the command line.

```
dotnet build Source/VGECEPatch.csproj -c Release -nodeReuse:false -p:UseSharedCompilation=false
```

This writes `Assemblies/VGECEPatch.dll` (net48, used by RimWorld 1.6 and 1.7), the only file to ship. Close
RimWorld first: it locks the DLL while the mod is active.

Layout:
- `Patches/`: XML patches always applied: asteroids and debris as CE projectiles (`VGE_SpaceProjectiles.xml`),
  shuttle and gauntlet ship interception pools (`CIWSPools.xml`), anticraft caster beam (`VGE_Anticraft.xml`).
- `ModPatches/VGE_Chapter2/`: Chapter 2 patches (compact turrets, anticraft emitter, combat vacsuit), loaded by `loadFolders.xml`
  only when Chapter 2 is active.
- `Source/Projectiles/`: the CE asteroid and debris projectile classes, their straight-line trajectory worker and
  their CIWS hit check.
- `Source/Patches/`: Harmony patches: asteroid spawning, CIWS targeting of asteroids, escape pods and gauntlet
  ships, gauntlet ship crews, VGE's point defense, gravship armor.
- `Source/CIWSPoolMultiplier.cs`, `Source/TargetDefs.cs`: interception pool scaling and the target def lists.

## License

CC BY-NC-SA 4.0, the same license as Combat Extended. Vanilla Gravship Expanded and Combat Extended belong to their
authors; see `LICENSE` for the parts of this patch that follow VGE's code.
