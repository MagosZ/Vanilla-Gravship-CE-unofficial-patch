using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace VGECEPatch
{
    // CE's anti-projectile verb (VerbCIWSProjectile) already lists every ProjectileCE on the map, but only accepts
    // overhead shells (flyOverhead) slower than its speed cap. VGE's asteroids and debris fly flat, so they are
    // accepted here by def.

    /// <summary>Accepts VGE asteroids and debris below the verb's speed cap.</summary>
    [HarmonyPatch(typeof(VerbProperties_CIWSProjectile), nameof(VerbProperties_CIWSProjectile.Interceptable), new[] { typeof(ThingDef) })]
    public static class VerbProperties_CIWSProjectile_Interceptable_Patch
    {
        public static void Postfix(VerbProperties_CIWSProjectile __instance, ThingDef __0, ref bool __result)
        {
            if (!__result && TargetDefs.IsSpaceProjectile(__0) && __0.projectile.speed < __instance.maximumSpeed)
            {
                __result = true;
            }
        }
    }

    /// <summary>Lists them in the turret's "Manage CIWS targets" window. They are targeted by default: CE only
    /// keeps a per-turret ignore list, which starts empty.</summary>
    [HarmonyPatch(typeof(VerbProperties_CIWSProjectile), "InitAllTargets")]
    public static class VerbProperties_CIWSProjectile_InitAllTargets_Patch
    {
        public static void Postfix(VerbProperties_CIWSProjectile __instance, ref IEnumerable<ThingDef> __result)
        {
            IEnumerable<ThingDef> ours = TargetDefs.SpaceProjectiles.Where(d => d.projectile.speed < __instance.maximumSpeed);
            __result = (__result ?? Enumerable.Empty<ThingDef>()).Concat(ours).Distinct().ToList();
        }
    }

    /// <summary>They are never friendly. CE's own check reads `thing.launcher.HostileTo(...)`, which would throw for
    /// these launcher-less projectiles on every target search.
    ///
    /// This is also where walls are checked. CE's CIWS aiming has no wall check (it expects targets in the sky), and
    /// VGE asteroids and debris fly at ground level, so a target only counts if CE's normal line of sight reaches the
    /// predicted intercept point (`Verb_LaunchProjectileCE.CanHitCellFromCellIgnoringRange`); otherwise it is skipped
    /// like a friendly. CE calls IsFriendlyTo only in `VerbCIWS.TryFindNewTarget`, just before it looks for a shot line.
    ///
    /// Not done by patching `VerbCIWS&lt;ProjectileCE&gt;.TryFindCEShootLineFromTo`: that method's compiled code is
    /// shared by every reference-type `VerbCIWS&lt;T&gt;`, and Harmony's copy of it hard-codes T = ProjectileCE, which
    /// stopped the skyfaller verbs from ever finding a shot line at a drop pod.</summary>
    [HarmonyPatch(typeof(VerbCIWSProjectile), "IsFriendlyTo", new[] { typeof(ProjectileCE) })]
    public static class VerbCIWSProjectile_IsFriendlyTo_Patch
    {
        private static Func<Verb_LaunchProjectileCE, Vector3, IntVec3, Thing, bool> canHitCellFromCell;
        private static bool lookedUp;

        public static bool Prefix(VerbCIWSProjectile __instance, ProjectileCE __0, ref bool __result)
        {
            if (__0 is ProjectileCE_VGESpace)
            {
                __result = !HasLineOfSight(__instance, __0);
                return false;
            }
            return true;
        }

        private static bool HasLineOfSight(VerbCIWSProjectile verb, ProjectileCE target)
        {
            if (!lookedUp)
            {
                lookedUp = true;
                var method = AccessTools.Method(typeof(Verb_LaunchProjectileCE), "CanHitCellFromCellIgnoringRange");
                if (method == null)
                {
                    Log.Error("[VGECEPatch] Verb_LaunchProjectileCE.CanHitCellFromCellIgnoringRange not found; CIWS turrets ignore walls when shooting at asteroids.");
                }
                else
                {
                    canHitCellFromCell = AccessTools.MethodDelegate<Func<Verb_LaunchProjectileCE, Vector3, IntVec3, Thing, bool>>(method);
                }
            }
            Thing caster = verb.caster;
            Map map = caster?.Map;
            if (map == null)
            {
                return false;
            }
            // CE's own intercept calculation; no intercept point means CE would skip the target anyway.
            if (!verb.TryFindCEShootLineFromTo(caster.Position, target, out _, out Vector3 targetPos))
            {
                return false;
            }
            IntVec3 interceptCell = targetPos.ToIntVec3();
            if (!interceptCell.InBounds(map))
            {
                return false;
            }
            if (canHitCellFromCell == null)
            {
                return true;
            }
            // Same shot source as CE uses for turrets: the turret's centre at its shot height.
            Vector3 casterPos = caster.DrawPos;
            var shotSource = new Vector3(casterPos.x, verb.ShotHeight, casterPos.z);
            return canHitCellFromCell(verb, shotSource, interceptCell, null);
        }
    }
}
