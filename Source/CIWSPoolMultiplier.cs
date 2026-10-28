using System.Linq;
using CombatExtended;
using Verse;

namespace VGECEPatch
{
    /// <summary>Scales the CE interception pool (`CompProperties_CIWSImpactHandler.HP`) a skyfaller inherits.
    /// Patches/CIWSPools.xml puts it on abstract parents (shuttles, gauntlet ships) so every child inherits it; a
    /// second pool comp in XML would give those defs two pools instead.</summary>
    public class CIWSPoolMultiplier : DefModExtension
    {
        public float multiplier = 1f;
    }

    /// <summary>Applies <see cref="CIWSPoolMultiplier"/> once the defs are loaded, before any map exists.</summary>
    [StaticConstructorOnStartup]
    public static class CIWSPoolMultipliers
    {
        static CIWSPoolMultipliers()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var extension = def.GetModExtension<CIWSPoolMultiplier>();
                if (extension == null)
                {
                    continue;
                }
                var pool = def.comps?.OfType<CompProperties_CIWSImpactHandler>().FirstOrDefault();
                if (pool == null)
                {
                    Log.Warning($"[VGECEPatch] {def.defName} has no CE interception pool to scale.");
                    continue;
                }
                pool.HP *= extension.multiplier;
            }
        }
    }
}
