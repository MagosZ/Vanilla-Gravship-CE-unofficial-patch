using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

namespace VGECEPatch
{
    /// <summary>VGE space debris as a CE projectile (replaces VGE's vanilla `Projectile_Space`). It flies flat at
    /// <see cref="FlightHeight"/>, collides and explodes by CE's rules, and drops VGE's loot when it hits something
    /// or is shot down by a CIWS turret, but not when it leaves the map.</summary>
    public class ProjectileCE_VGESpace : ProjectileCE_Explosive
    {
        /// <summary>CE height units. Above the ground (CE treats height &lt;= 0 as hitting the ground) and below a
        /// downed human (0.09), so walls, standing and downed pawns are all in the way.</summary>
        public const float FlightHeight = 0.02f;

        private bool dropLoot;

        /// <summary>Launches flat from <paramref name="origin"/> along <paramref name="direction"/> at the def's
        /// speed, with no launcher (as VGE does).</summary>
        public void LaunchFlat(Vector3 origin, Vector3 direction)
        {
            float rotation = TrajectoryWorker.ShotRotation(Props, origin, origin + direction);
            Launch(null, new Vector2(origin.x, origin.z), 0f, rotation, FlightHeight, def.projectile.speed);
        }

        public override void Impact(Thing hitThing)
        {
            dropLoot = true;
            base.Impact(hitThing);
        }

        /// <summary>Called by <see cref="CompCIWSImpactHandler_VGESpace"/> just before a CIWS turret destroys it.</summary>
        public void Notify_ShotDown()
        {
            dropLoot = true;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (dropLoot && Map != null)
            {
                dropLoot = false;
                TryDropLoot();
            }
            base.Destroy(mode);
        }

        /// <summary>VGE's small and medium debris drop nothing.</summary>
        protected virtual void TryDropLoot()
        {
        }
    }

    /// <summary>VGE's large debris (`Projectile_SpaceDebris`): a 25% chance of a steel slag chunk.</summary>
    public class ProjectileCE_VGESpaceDebris : ProjectileCE_VGESpace
    {
        protected override void TryDropLoot()
        {
            if (Rand.Chance(0.25f))
            {
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), Position, Map, ThingPlaceMode.Near);
            }
        }
    }

    /// <summary>VGE's asteroids (`Projectile_Asteroid`): a vacstone chunk, and from a medium (10%) or large (25%)
    /// asteroid a forbidden stack of steel, uranium, gold or plasteel.</summary>
    public class ProjectileCE_VGEAsteroid : ProjectileCE_VGESpace
    {
        protected override void TryDropLoot()
        {
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.ChunkVacstone), Position, Map, ThingPlaceMode.Near);

            float chance = def.defName switch
            {
                "VGE_MediumAsteroid" => 0.10f,
                "VGE_LargeAsteroid" => 0.25f,
                _ => 0f,
            };
            if (Rand.Value < chance)
            {
                Thing resource = MakeResourceLoot();
                GenPlace.TryPlaceThing(resource, Position, Map, ThingPlaceMode.Near);
                resource.SetForbidden(true, false);
            }
        }

        private static Thing MakeResourceLoot()
        {
            ThingDef resourceDef = new[] { ThingDefOf.Steel, ThingDefOf.Uranium, ThingDefOf.Gold, ThingDefOf.Plasteel }.RandomElement();
            Thing resource = ThingMaker.MakeThing(resourceDef);
            if (resourceDef == ThingDefOf.Steel || resourceDef == ThingDefOf.Uranium)
            {
                resource.stackCount = Rand.RangeInclusive(5, 15);
            }
            else if (resourceDef == ThingDefOf.Gold)
            {
                resource.stackCount = Rand.RangeInclusive(10, 30);
            }
            else
            {
                resource.stackCount = Rand.RangeInclusive(1, 5);
            }
            return resource;
        }
    }

    /// <summary>The 5 HP interception pool on VGE asteroids and debris (Patches/VGE_SpaceProjectiles.xml). When
    /// it runs out the projectile is destroyed without exploding and drops its loot.</summary>
    public class CompCIWSImpactHandler_VGESpace : CompCIWSImpactHandler
    {
        protected override void OnDestroying(DamageInfo dinfo)
        {
            (parent as ProjectileCE_VGESpace)?.Notify_ShotDown();
            base.OnDestroying(dinfo);
        }
    }
}
