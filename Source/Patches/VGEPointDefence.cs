using System;
using HarmonyLib;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;

namespace VGECEPatch
{
    // VGE's own gravship point-defense turret (CompPointDefence) shoots asteroids and debris by checking
    // `t is Projectile_Space`, and its hit chance only handles vanilla `Projectile`s. Once they are CE projectiles
    // both checks fail, so these patches give it back exactly its VGE behaviour for them.

    /// <summary>VGE's point defense accepts VGE asteroids and debris again (VGE accepts every one, any faction).</summary>
    [HarmonyPatch(typeof(CompPointDefence), "IsValidProjectile")]
    public static class CompPointDefence_IsValidProjectile_Patch
    {
        public static void Postfix(Thing __0, ref bool __result)
        {
            if (!__result && __0 is ProjectileCE_VGESpace)
            {
                __result = true;
            }
        }
    }

    /// <summary>VGE's hit chance for them, computed on VGE's original vanilla speed (CE speed / 0.6, see
    /// Patches/VGE_SpaceProjectiles.xml). A hit drops the loot, as VGE's projectiles did whenever they were destroyed.</summary>
    [HarmonyPatch(typeof(CompPointDefence), "InterceptChance")]
    public static class CompPointDefence_InterceptChance_Patch
    {
        /// <summary>CE speed = VGE speed x this.</summary>
        public const float CESpeedFactor = 0.6f;

        public static void Postfix(CompPointDefence __instance, Thing __0, ref bool __result)
        {
            if (!(__0 is ProjectileCE_VGESpace projectile))
            {
                return;
            }
            float velocity = projectile.def.projectile.speed / CESpeedFactor;
            float chance;
            if (__instance.Props.isEnemyPointDefense)
            {
                chance = 0.735f * (float)Math.Exp(-Math.Max(0, velocity - 30) / __instance.Props.falloffScale);
            }
            else
            {
                chance = 0.98f * (float)Math.Exp(-Math.Max(0, velocity - 30) / 60f);
            }
            __result = Rand.Chance(Mathf.Clamp(chance, 0.05f, 0.98f));
            if (__result)
            {
                projectile.Notify_ShotDown();
            }
        }
    }
}
