using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using VanillaGravshipExpanded;
using Verse;

namespace VGECEPatch
{
    // VGE's escape pod and VFE Pirates' gauntlet raid ships are plain Skyfallers, not transport pods, so CE's
    // anti-skyfaller verb never lists them. Both already carry CE's drop pod interception pool (22 HP, from
    // SkyfallerBase), so they only need to become targets. CE would call the escape pod friendly because its only
    // content is a faction-less building; a gauntlet ship's crashed-ship building has the raiders' faction, so CE's
    // own check is right for it (an enemy ship is hostile, a friendly gauntlet raid is left alone).

    /// <summary>Adds spawned escape pods and gauntlet ships to the skyfallers a CIWS skyfaller verb looks at.</summary>
    [HarmonyPatch(typeof(VerbCIWSSkyfaller), nameof(VerbCIWSSkyfaller.Targets), MethodType.Getter)]
    public static class VerbCIWSSkyfaller_Targets_Patch
    {
        public static void Postfix(VerbCIWSSkyfaller __instance, ref IEnumerable<Skyfaller> __result)
        {
            Map map = __instance.Caster?.Map;
            if (map == null)
            {
                return;
            }
            List<Skyfaller> pods = null;
            foreach (ThingDef def in TargetDefs.ExtraSkyfallers)
            {
                List<Thing> things = map.listerThings.ThingsOfDef(def);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Skyfaller pod)
                    {
                        (pods ??= new List<Skyfaller>()).Add(pod);
                    }
                }
            }
            if (pods != null)
            {
                __result = (__result ?? Enumerable.Empty<Skyfaller>()).Concat(pods);
            }
        }
    }

    /// <summary>Escape pods are never friendly to a CIWS turret.</summary>
    [HarmonyPatch(typeof(VerbCIWSSkyfaller), "IsFriendlyTo", new[] { typeof(Skyfaller) })]
    public static class VerbCIWSSkyfaller_IsFriendlyTo_Patch
    {
        public static bool Prefix(Skyfaller __0, ref bool __result)
        {
            if (__0 is Skyfaller_EscapePod)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    /// <summary>Lists escape pods and gauntlet ships in the turret's "Manage CIWS targets" window. They are targeted by default:
    /// CE only keeps a per-turret ignore list, which starts empty.</summary>
    [HarmonyPatch(typeof(VerbProperties_CIWSSkyfaller), "InitAllTargets")]
    public static class VerbProperties_CIWSSkyfaller_InitAllTargets_Patch
    {
        public static void Postfix(ref IEnumerable<ThingDef> __result)
        {
            __result = (__result ?? Enumerable.Empty<ThingDef>()).Concat(TargetDefs.ExtraSkyfallers).Distinct().ToList();
        }
    }
}
