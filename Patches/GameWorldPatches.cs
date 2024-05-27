using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnDestroy));
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            Plugin.Instance.Map?.OnRaidEnd();
            AirdropBoxOnBoxLandPatch.OnRaidEnd();
        }
    }
}
