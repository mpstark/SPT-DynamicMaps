using System;
using System.Collections.Generic;
using System.Reflection;
using Aki.Custom.Airdrops;
using Aki.Reflection.Patching;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class AirdropBoxOnBoxLandPatch : ModulePatch
    {
        internal static event Action<AirdropBox> OnAirdropLanded;
        internal static List<AirdropBox> Airdrops = new List<AirdropBox>();

        protected override MethodBase GetTargetMethod()
        {
            GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;

            // thanks to TechHappy for the breadcrumb of what method to patch
            return AccessTools.Method(typeof(AirdropBox), "OnBoxLand");
        }

        [PatchPostfix]
        public static void PatchPostfix(AirdropBox __instance)
        {
            Airdrops.Add(__instance);
            OnAirdropLanded?.Invoke(__instance);
        }

        internal static void OnRaidEnd()
        {
            Airdrops.Clear();
        }
    }
}
