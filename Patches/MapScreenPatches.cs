using System.Reflection;
using Aki.Reflection.Patching;
using DynamicMaps.Config;
using EFT.UI.Map;
using HarmonyLib;

namespace DynamicMaps.Patches
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
            if (!Settings.Enabled.Value)
            {
                // mod is disabled
                Plugin.Instance.Map?.Close();
                return true;
            }

            // show instead
            Plugin.Instance.TryAttachToMapScreen(__instance);
            Plugin.Instance.Map?.Show();
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
            if (!Settings.Enabled.Value)
            {
                // mod is disabled
                return true;
            }

            // show instead
            Plugin.Instance.Map?.Close();
            return false;
        }
    }
}
