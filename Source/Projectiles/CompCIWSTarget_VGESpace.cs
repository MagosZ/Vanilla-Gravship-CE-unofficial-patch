using System.Collections.Generic;
using CombatExtended;
using UnityEngine;
using Verse;

namespace VGECEPatch
{
    /// <summary>A swept, size-based hit check for CIWS rounds fired at VGE asteroids and debris.
    /// CE's default for projectile targets only tests the round's end-of-tick position against a fixed 1-cell circle,
    /// so a fast round can step over the target and a large asteroid is no easier to hit than a small one. This counts
    /// a hit when the round passes within <see cref="HitRadius"/> of the target at any point during the tick, taking
    /// both movements into account. The radius is set in XML (<see cref="CompProperties_CIWSTarget_VGESpace"/>).
    /// It is CE's own hook for this (`ProjectileCE_CIWS.CanCollideWith` asks the target's comp first). The comp also
    /// lands in CE's comp-target list, but only `VerbCIWS_CompSkyfaller` reads that list, and only for its own comp
    /// type, so no turret gains or loses the asteroids as targets.</summary>
    public class CompCIWSTarget_VGESpace : CompCIWSTarget
    {
        // Not CE's `Props`: that casts to `CompProperties_CIWSTarget`, which reports a config error for any subclass
        // comp (its check is inverted), so these properties derive from plain `CompProperties`.
        private CompProperties_CIWSTarget_VGESpace SpaceProps => (CompProperties_CIWSTarget_VGESpace)props;

        public float HitRadius => (parent.def.graphicData?.drawSize.x ?? 1f) * SpaceProps.radiusFactor + SpaceProps.radiusBonus;

        /// <summary>Only called by CE's comp-based CIWS verbs, which never target these.</summary>
        public override bool IsFriendlyTo(Thing thing) => false;

        public override IEnumerable<Vector3> PredictedPositions
        {
            get
            {
                var projectile = (ProjectileCE)parent;
                return projectile.TrajectoryWorker.PredictPositions(projectile, GenTicks.TicksPerRealSecond);
            }
        }

        public override bool? CanCollideWith(ProjectileCE_CIWS round, out float dist)
        {
            dist = -1f;
            if (!(parent is ProjectileCE target))
            {
                return null;
            }
            // Horizontal positions, as CE's default check: both this tick's segments, solved for the closest approach.
            Vector2 roundStart = new Vector2(round.LastPos.x, round.LastPos.z);
            Vector2 roundMove = new Vector2(round.ExactPosition.x, round.ExactPosition.z) - roundStart;
            Vector2 targetStart = new Vector2(target.LastPos.x, target.LastPos.z);
            Vector2 targetMove = new Vector2(target.ExactPosition.x, target.ExactPosition.z) - targetStart;

            Vector2 offset = roundStart - targetStart;
            Vector2 relativeMove = roundMove - targetMove;
            float moveSqr = relativeMove.sqrMagnitude;
            float t = moveSqr > 1e-6f ? Mathf.Clamp01(-Vector2.Dot(offset, relativeMove) / moveSqr) : 0f;
            float radius = HitRadius;
            if ((offset + relativeMove * t).sqrMagnitude >= radius * radius)
            {
                return false;
            }
            // CE places the impact at this distance along the round's shot line (from LastPos, normalised).
            dist = t * (round.ExactPosition - round.LastPos).magnitude;
            return true;
        }
    }

    /// <summary>Hit radius = drawSize × <see cref="radiusFactor"/> + <see cref="radiusBonus"/>, in cells.</summary>
    public class CompProperties_CIWSTarget_VGESpace : CompProperties
    {
        public float radiusFactor = 0.5f;
        public float radiusBonus = 0.5f;

        public CompProperties_CIWSTarget_VGESpace()
        {
            compClass = typeof(CompCIWSTarget_VGESpace);
        }
    }
}
