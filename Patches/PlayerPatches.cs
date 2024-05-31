using System;
using System.Reflection;
using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;

namespace DynamicMaps.Patches
{
    internal class PlayerOnDeadPatch : ModulePatch
    {
        internal static event Action<Player> OnDead;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnDead));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance)
        {
            OnDead?.Invoke(__instance);
        }
    }
}
