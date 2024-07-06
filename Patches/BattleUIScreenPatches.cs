using System;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.UI;
using HarmonyLib;
using EFT.UI.Screens;
using static EFT.UI.EftBattleUIScreen;

namespace DynamicMaps.Patches
{
    internal class BattleUIScreenShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BattleUIScreen<GClass3136, EEftScreenType>),
                                      nameof(BattleUIScreen<GClass3136, EEftScreenType>.Show),
                                      new Type[] { typeof(GamePlayerOwner) });
        }

        [PatchPostfix]
        public static void PatchPostfix(BattleUIScreen<GClass3136, EEftScreenType> __instance)
        {
            Plugin.Instance.TryAttachToBattleUIScreen(__instance);
        }
    }
}
