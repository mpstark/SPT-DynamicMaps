using System.Reflection;
using Aki.Reflection.Patching;
using EFT.UI.Map;
using HarmonyLib;

namespace InGameMap.Patches
{
    internal class MapScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MapScreen), nameof(MapScreen.Show));
        }

        [PatchPrefix]
        public static bool PatchPrefix(MapScreen __instance)
        {
            // show instead
            Plugin.Instance.TryAttachToMapScreen(__instance);
            Plugin.Instance.Map.Show();
            return false;
        }
    }

    internal class MapScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MapScreen), nameof(MapScreen.Close));
        }

        [PatchPrefix]
        public static bool PatchPrefix(MapScreen __instance)
        {
            // show instead
            Plugin.Instance.Map.Close();
            return false;
        }
    }
}
