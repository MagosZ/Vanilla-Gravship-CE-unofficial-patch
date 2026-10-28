using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VanillaGravshipExpanded;
using Verse;

namespace VGECEPatch
{
    /// <summary>The defs this mod makes CIWS targets, found by thingClass once the defs are loaded.</summary>
    public static class TargetDefs
    {
        /// <summary>VFE Pirates' gauntlet raid ships (`VFEPirates.IncomingSmoker` skyfallers carrying a crashed-ship
        /// building with the raiders). Named, not found by class: the same class also drops the "low orbit crash"
        /// scenario's ship chunks, which aren't part of the raid.</summary>
        private static readonly string[] GauntletShipDefNames = { "VFEP_Ship_Black", "VFEP_Ship_Green", "VFEP_Ship_Orange", "VFEP_Ship_Red" };

        private static List<ThingDef> escapePods;
        private static List<ThingDef> gauntletShips;
        private static List<ThingDef> extraSkyfallers;
        private static List<ThingDef> spaceProjectiles;

        /// <summary>VGE escape pod skyfallers (`Skyfaller_EscapePod` or a subclass).</summary>
        public static List<ThingDef> EscapePods => escapePods ??= DefDatabase<ThingDef>.AllDefsListForReading
            .Where(d => d.thingClass != null && typeof(Skyfaller_EscapePod).IsAssignableFrom(d.thingClass))
            .ToList();

        /// <summary>VFE Pirates' gauntlet ships; empty when VFE Pirates isn't loaded.</summary>
        public static List<ThingDef> GauntletShips => gauntletShips ??= GauntletShipDefNames
            .Select(name => DefDatabase<ThingDef>.GetNamedSilentFail(name))
            .Where(d => d?.thingClass != null && typeof(Skyfaller).IsAssignableFrom(d.thingClass))
            .ToList();

        /// <summary>Skyfallers CE's anti-skyfaller verb doesn't list by itself (they aren't transport pods).</summary>
        public static List<ThingDef> ExtraSkyfallers => extraSkyfallers ??= EscapePods.Concat(GauntletShips).ToList();

        public static bool IsGauntletShip(ThingDef def) => def != null && GauntletShips.Contains(def);

        /// <summary>VGE asteroids and space debris, turned into CE projectiles by Patches/VGE_SpaceProjectiles.xml.</summary>
        public static List<ThingDef> SpaceProjectiles => spaceProjectiles ??= DefDatabase<ThingDef>.AllDefsListForReading
            .Where(IsSpaceProjectile)
            .ToList();

        public static bool IsSpaceProjectile(ThingDef def)
        {
            return def?.thingClass != null && def.projectile != null
                && typeof(ProjectileCE_VGESpace).IsAssignableFrom(def.thingClass);
        }
    }
}
