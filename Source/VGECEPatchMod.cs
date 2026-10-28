using System;
using HarmonyLib;
using Verse;

namespace VGECEPatch
{
    public class VGECEPatchMod : Mod
    {
        public const string HarmonyId = "you.vgecepatch.unofficial";

        public static Harmony HarmonyInstance;

        public VGECEPatchMod(ModContentPack content) : base(content)
        {
            HarmonyInstance = new Harmony(HarmonyId);
            PatchAllClasses();
        }

        /// <summary>Patches each class on its own, so one missing target can't stop the rest. Reading a class's
        /// attributes resolves the types they name, so that happens inside the per-class try too.</summary>
        private static void PatchAllClasses()
        {
            foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(VGECEPatchMod).Assembly))
            {
                try
                {
                    if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                    {
                        continue;
                    }
                    HarmonyInstance.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    Log.Error($"[VGECEPatch] Failed to apply patch class {type.FullName}: {e}");
                }
            }
        }
    }
}
