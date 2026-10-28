using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;

namespace VGECEPatch
{
    // VGE spawns each asteroid or piece of debris with `(Projectile)GenSpawn.Spawn(...)` and launches it with
    // vanilla's Launch, which fails for a CE projectile. These prefixes replace the two spawn methods with copies
    // that keep VGE's rules and VGE's own type pickers (so the phase weights stay VGE's), then launch the CE way.
    // If the picked def is not one of ours (the XML patch didn't apply), VGE's original runs.

    internal static class SpaceProjectileSpawner
    {
        /// <summary>`GameCondition.SingleMap` is protected; VGE's spawn methods use it.</summary>
        public static readonly Func<GameCondition, Map> SingleMap =
            AccessTools.MethodDelegate<Func<GameCondition, Map>>(AccessTools.PropertyGetter(typeof(GameCondition), "SingleMap"));

        public static void Spawn(ThingDef def, IntVec3 spawnCell, Map map, float angle)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
            var projectile = (ProjectileCE_VGESpace)GenSpawn.Spawn(def, spawnCell, map);
            projectile.LaunchFlat(spawnCell.ToVector3Shifted(), direction);
        }
    }

    /// <summary>Asteroid shower: a random standable unroofed edge cell, aimed at the map centre +-25 degrees.</summary>
    [HarmonyPatch(typeof(GameCondition_AsteroidShower), "SpawnProjectile")]
    public static class GameCondition_AsteroidShower_SpawnProjectile_Patch
    {
        private static Func<GameCondition_AsteroidShower, ThingDef> getRandomAsteroidType;

        public static bool Prepare()
        {
            var method = AccessTools.Method(typeof(GameCondition_AsteroidShower), "GetRandomAsteroidType");
            if (method == null)
            {
                Log.Error("[VGECEPatch] GameCondition_AsteroidShower.GetRandomAsteroidType not found; asteroids stay vanilla projectiles.");
                return false;
            }
            getRandomAsteroidType = AccessTools.MethodDelegate<Func<GameCondition_AsteroidShower, ThingDef>>(method);
            return true;
        }

        public static bool Prefix(GameCondition_AsteroidShower __instance)
        {
            ThingDef asteroidType = getRandomAsteroidType(__instance);
            if (!TargetDefs.IsSpaceProjectile(asteroidType))
            {
                return true;
            }
            Map map = SpaceProjectileSpawner.SingleMap(__instance);
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Roofed(map), map, 0f, out IntVec3 spawnCell))
            {
                return false;
            }
            Vector3 directionToCenter = (map.Center.ToVector3() - spawnCell.ToVector3()).normalized;
            float baseAngle = Quaternion.LookRotation(directionToCenter).eulerAngles.y;
            SpaceProjectileSpawner.Spawn(asteroidType, spawnCell, map, baseAngle + Rand.Range(-25f, 25f));
            return false;
        }
    }

    /// <summary>Space debris: a standable unroofed cell on the event's chosen map edge, along the event's base
    /// angle +-5 degrees.</summary>
    [HarmonyPatch(typeof(GameCondition_SpaceDebris), "SpawnProjectile")]
    public static class GameCondition_SpaceDebris_SpawnProjectile_Patch
    {
        private static Func<GameCondition_SpaceDebris, ThingDef> getRandomDebrisType;
        private static AccessTools.FieldRef<GameCondition_SpaceDebris, Rot4> spawnDirection;
        private static AccessTools.FieldRef<GameCondition_SpaceDebris, float> baseAngle;

        public static bool Prepare()
        {
            var method = AccessTools.Method(typeof(GameCondition_SpaceDebris), "GetRandomDebrisType");
            var directionField = AccessTools.Field(typeof(GameCondition_SpaceDebris), "spawnDirection");
            var angleField = AccessTools.Field(typeof(GameCondition_SpaceDebris), "baseAngle");
            if (method == null || directionField == null || angleField == null)
            {
                Log.Error("[VGECEPatch] GameCondition_SpaceDebris members not found; space debris stays vanilla projectiles.");
                return false;
            }
            getRandomDebrisType = AccessTools.MethodDelegate<Func<GameCondition_SpaceDebris, ThingDef>>(method);
            spawnDirection = AccessTools.FieldRefAccess<GameCondition_SpaceDebris, Rot4>(directionField);
            baseAngle = AccessTools.FieldRefAccess<GameCondition_SpaceDebris, float>(angleField);
            return true;
        }

        public static bool Prefix(GameCondition_SpaceDebris __instance)
        {
            ThingDef debrisType = getRandomDebrisType(__instance);
            if (!TargetDefs.IsSpaceProjectile(debrisType))
            {
                return true;
            }
            Map map = SpaceProjectileSpawner.SingleMap(__instance);
            if (!CellFinder.TryFindRandomEdgeCellWith(c => c.Standable(map) && !c.Roofed(map), map, spawnDirection(__instance), 0f, out IntVec3 spawnCell))
            {
                return false;
            }
            SpaceProjectileSpawner.Spawn(debrisType, spawnCell, map, baseAngle(__instance) + Rand.Range(-5f, 5f));
            return false;
        }
    }
}
