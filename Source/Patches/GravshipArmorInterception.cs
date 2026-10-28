using CombatExtended;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VGECEPatch
{
    // VGE Chapter 2's gravship armor catches hostile fire: VGE retargets any vanilla projectile whose path crosses
    // armor owned by a faction hostile to the shooter onto that armor cell. That patch is on vanilla
    // `Projectile.Launch`, so CE projectiles (`ProjectileCE` is not a `Projectile`) pass straight over the armor.
    // This gives them the same rule, checked each tick along the path instead of at launch (retargeting a CE shell
    // would mean recomputing its arc). VGE's own vanilla projectiles are still handled by VGE.

    /// <summary>After CE's own collision check finds nothing this tick, a hostile CE projectile that crossed gravship
    /// armor detonates on the first armor cell it crossed. Height is ignored, as in VGE. CE CIWS rounds override the
    /// check and are left alone; a projectile without a shooter (VGE's asteroids, or a shell whose shooter was
    /// destroyed before a save) is skipped, as VGE skips them.</summary>
    [HarmonyPatch(typeof(ProjectileCE), "CheckForCollisionBetween")]
    public static class ProjectileCE_CheckForCollisionBetween_GravshipArmor_Patch
    {
        /// <summary>Detonation height, below CE's wall height (2): an explosion centred on a wall cell then only
        /// reaches the side the shell came from (`GenExplosionCE` needLOSToCell). At or above wall height CE would
        /// hit every unroofed cell in the radius, the ship's interior included.</summary>
        private const float DetonationHeight = CollisionVertical.WallCollisionHeight / 2f;

        private static ThingDef armorDef;
        private static bool armorDefLooked;

        /// <summary>VGE Chapter 2's armor, or null when Chapter 2 isn't loaded.</summary>
        private static ThingDef ArmorDef
        {
            get
            {
                if (!armorDefLooked)
                {
                    armorDef = DefDatabase<ThingDef>.GetNamedSilentFail("VGE_GravshipArmor");
                    armorDefLooked = true;
                }
                return armorDef;
            }
        }

        public static void Postfix(ProjectileCE __instance, ref bool __result)
        {
            if (__result || __instance.landed || __instance.Destroyed || ArmorDef == null)
            {
                return;
            }
            Faction shooterFaction = __instance.launcher?.Faction;
            Map map = __instance.Map;
            if (shooterFaction == null || map == null)
            {
                return;
            }
            Thing armor = FirstHostileArmorCrossed(__instance.LastPos.ToIntVec3(), __instance.ExactPosition.ToIntVec3(), map, shooterFaction);
            if (armor == null)
            {
                return;
            }
            __instance.ExactPosition = new Vector3(armor.Position.x + 0.5f, DetonationHeight, armor.Position.z + 0.5f);
            __instance.landed = true;
            __instance.Impact(armor);
            __result = true;
        }

        /// <summary>The first gravship armor on the line from <paramref name="from"/> to <paramref name="to"/>
        /// (both included) whose owner is hostile to <paramref name="shooterFaction"/>.</summary>
        private static Thing FirstHostileArmorCrossed(IntVec3 from, IntVec3 to, Map map, Faction shooterFaction)
        {
            Thing armor = HostileArmorAt(from, map, shooterFaction);
            if (armor != null)
            {
                return armor;
            }
            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(from, to))
            {
                armor = HostileArmorAt(cell, map, shooterFaction);
                if (armor != null)
                {
                    return armor;
                }
            }
            return HostileArmorAt(to, map, shooterFaction);
        }

        private static Thing HostileArmorAt(IntVec3 cell, Map map, Faction shooterFaction)
        {
            if (!cell.InBounds(map))
            {
                return null;
            }
            Building edifice = cell.GetEdifice(map);
            if (edifice == null || edifice.def != ArmorDef || edifice.Faction == null)
            {
                return null;
            }
            return edifice.Faction.HostileTo(shooterFaction) ? edifice : null;
        }
    }
}
