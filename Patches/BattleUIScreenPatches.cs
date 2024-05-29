using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using EFT.UI;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class BattleUIScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BattleUIScreen),
                                      nameof(BattleUIScreen.Show),
                                      new Type[] { typeof(GamePlayerOwner) });
        }

        [PatchPostfix]
        public static void PatchPostfix(BattleUIScreen __instance)
        {
            Plugin.Instance.TryAttachToBattleUIScreen(__instance);
        }
    }
}
