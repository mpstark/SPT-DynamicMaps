using System.Reflection;
using SPT.Reflection.Patching;
using EFT.UI;
using EFT.UI.Map;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class CommonUIAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CommonUI), nameof(CommonUI.Awake));
        }

        [PatchPostfix]
        public static void PatchPostfix(CommonUI __instance)
        {
            var mapScreen = Traverse.Create(__instance.InventoryScreen).Field("_mapScreen").GetValue<MapScreen>();

            Plugin.Instance.TryAttachToMapScreen(mapScreen);
        }
    }
}
