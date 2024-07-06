using System;
using System.Collections.Generic;
using System.Reflection;
using SPT.Custom.Airdrops;
using SPT.Reflection.Patching;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class AirdropBoxOnBoxLandPatch : ModulePatch
    {
        internal static event Action<AirdropBox> OnAirdropLanded;
        internal static List<AirdropBox> Airdrops = new List<AirdropBox>();

        private bool _hasRegisteredEvents = false;

        protected override MethodBase GetTargetMethod()
        {
            if (!_hasRegisteredEvents)
            {
                GameWorldOnDestroyPatch.OnRaidEnd += OnRaidEnd;
                _hasRegisteredEvents = true;
            }

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
