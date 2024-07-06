using System;
using System.Reflection;
using SPT.Reflection.Patching;
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
        public static bool PatchPrefix()
        {
            try
            {
                if (!Settings.ReplaceMapScreen.Value)
                {
                    // mod is disabled
                    Plugin.Instance.Map?.OnMapScreenClose();
                    return true;
                }

                // show instead
                Plugin.Instance.Map?.OnMapScreenShow();
                return false;
            }
            catch(Exception e)
            {
                Plugin.Log.LogError($"Caught error while trying to show map");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");

                return true;
            }
        }
    }

    internal class MapScreenClosePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MapScreen), nameof(MapScreen.Close));
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            try
            {
                if (!Settings.ReplaceMapScreen.Value)
                {
                    // mod is disabled
                    return true;
                }

                // close instead
                Plugin.Instance.Map?.OnMapScreenClose();
                return false;
            }
            catch(Exception e)
            {
                Plugin.Log.LogError($"Caught error while trying to close map");
                Plugin.Log.LogError($"{e.Message}");
                Plugin.Log.LogError($"{e.StackTrace}");

                return true;
            }
        }
    }
}
