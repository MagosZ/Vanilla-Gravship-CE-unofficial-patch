using System.Collections.Generic;
using System.Linq;
using CombatExtended;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VGECEPatch
{
    // A gauntlet ship's raiders sit inside its crashed-ship building, and CE only looks for pawns directly in a pod
    // or in nested pods. Without these patches a hit would never wound them and a shot-down ship would delete them
    // with no corpses. They get CE's drop pod rule instead: each hit wounds a random raider; when the ship is
    // destroyed every raider falls out and dies. CE's own handler then drops the wreck's leavings and removes the ship.

    internal static class GauntletShipCrew
    {
        /// <summary>The container holding the raiders: the crashed-ship building inside the skyfaller.</summary>
        public static ThingOwner CrewOwner(Thing ship)
        {
            ThingOwner held = (ship as IThingHolder)?.GetDirectlyHeldThings();
            if (held == null)
            {
                return null;
            }
            foreach (Thing thing in held)
            {
                if (!(thing is Pawn) && thing is IThingHolder building)
                {
                    return building.GetDirectlyHeldThings();
                }
            }
            return null;
        }
    }

    /// <summary>Each hit on a gauntlet ship wounds a random raider inside, as CE does for drop pods.</summary>
    [HarmonyPatch(typeof(CompCIWSImpactHandler_Skyfaller), nameof(CompCIWSImpactHandler_Skyfaller.OnImpact))]
    public static class CompCIWSImpactHandler_Skyfaller_OnImpact_Patch
    {
        public static void Postfix(CompCIWSImpactHandler_Skyfaller __instance, DamageInfo dinfo)
        {
            Thing ship = __instance.parent;
            if (ship.Destroyed || !TargetDefs.IsGauntletShip(ship.def))
            {
                return;
            }
            List<Pawn> crew = GauntletShipCrew.CrewOwner(ship)?.OfType<Pawn>().ToList();
            if (crew != null && crew.Count > 0)
            {
                crew.RandomElement().TakeDamage(dinfo);
            }
        }
    }

    /// <summary>A shot-down gauntlet ship drops its raiders where it was hit, and they die, as CE does for drop
    /// pods. Runs before CE's own handler, which then leaves the wreck's leavings and removes the ship.</summary>
    [HarmonyPatch(typeof(CompCIWSImpactHandler_Skyfaller), "OnDestroying")]
    public static class CompCIWSImpactHandler_Skyfaller_OnDestroying_Patch
    {
        public static void Prefix(CompCIWSImpactHandler_Skyfaller __instance, DamageInfo dinfo)
        {
            Thing ship = __instance.parent;
            Map map = ship.Map;
            if (map == null || !TargetDefs.IsGauntletShip(ship.def))
            {
                return;
            }
            ThingOwner owner = GauntletShipCrew.CrewOwner(ship);
            if (owner == null)
            {
                return;
            }
            // Same drop spot as CE: under where the ship is drawn, or its landing cell if that is off the map.
            IntVec3 dropCell = ship.DrawPos.ToIntVec3();
            if (!dropCell.InBounds(map))
            {
                dropCell = ship.Position;
            }
            foreach (Pawn pawn in owner.OfType<Pawn>().ToList())
            {
                if (!owner.TryDrop(pawn, dropCell, map, ThingPlaceMode.Near, out _))
                {
                    continue;
                }
                pawn.TakeDamage(dinfo);
                if (!pawn.Dead)
                {
                    pawn.Kill(dinfo);
                }
            }
        }
    }
}
