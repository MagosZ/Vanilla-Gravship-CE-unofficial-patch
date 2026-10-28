using System.Collections.Generic;
using CombatExtended;
using UnityEngine;

namespace VGECEPatch
{
    /// <summary>A straight line at the launch height, at constant speed: VGE's asteroids and debris fly flat across
    /// the map. CE's gravity-free settings can't do this (the flight time to height 0 becomes infinite), so the
    /// flight time is the time to cover <see cref="FlightDistance"/>; the projectile leaves the map long before.
    /// CIWS turrets aim with <see cref="PredictPositions"/>.</summary>
    public class StraightTrajectoryWorker : BaseTrajectoryWorker
    {
        /// <summary>Same reach as VGE's own launch target, 1000 cells away.</summary>
        public const float FlightDistance = 1000f;

        public override Vector3 MoveForward(ProjectileCE projectile)
        {
            Vector3 next = projectile.ExactPosition + projectile.velocity;
            next.y = projectile.shotHeight;
            return next;
        }

        public override IEnumerable<Vector3> PredictPositions(ProjectileCE projectile, int tickCount)
        {
            Vector3 pos = projectile.ExactPosition;
            int end = Mathf.Min(tickCount, projectile.ticksToImpact);
            for (int i = 0; i < end; i++)
            {
                pos += projectile.velocity;
                pos.y = projectile.shotHeight;
                yield return pos;
            }
        }

        public override float DistanceTraveled(float shotHeight, float shotSpeed, float shotAngle, float gravityPerWidth)
        {
            return FlightDistance;
        }

        public override float GetFlightTime(float shotAngle, float shotSpeed, float gravityPerWidth, float shotHeight)
        {
            return FlightDistance / Mathf.Max(shotSpeed, 0.01f);
        }
    }
}
