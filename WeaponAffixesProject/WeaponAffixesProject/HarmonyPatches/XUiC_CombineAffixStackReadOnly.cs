using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemStack))]
    public static class XUiC_CombineAffixStackReadOnly
    {
        [HarmonyPatch(nameof(XUiC_ItemStack.HandleClickComplete))]
        [HarmonyPrefix]
        public static bool HandleClickCompletePrefix(XUiC_ItemStack __instance)
        {
            if (__instance is XUiC_CombineAffixStack affixStack)
            {
                affixStack.ToggleSelection();
                return false;
            }

            if (__instance is XUiC_RamAffixStack ramAffixStack)
            {
                ramAffixStack.ToggleSelection();
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
        [HarmonyPrefix]
        public static bool HandleMoveToPreferredLocationPrefix(XUiC_ItemStack __instance)
        {
            if (__instance is XUiC_CombineAffixStack affixStack)
            {
                affixStack.ToggleSelection();
                return false;
            }

            if (__instance is XUiC_RamAffixStack ramAffixStack)
            {
                ramAffixStack.ToggleSelection();
                return false;
            }

            return true;
        }

        [HarmonyPatch(nameof(XUiC_ItemStack.HandlePartialStackPickup))]
        [HarmonyPrefix]
        public static bool HandlePartialStackPickupPrefix(XUiC_ItemStack __instance)
        {
            if (__instance is XUiC_CombineAffixStack || __instance is XUiC_RamAffixStack)
                return false;

            return true;
        }

        [HarmonyPatch(nameof(XUiC_ItemStack.HandleStackSwap))]
        [HarmonyPrefix]
        public static bool HandleStackSwapPrefix(XUiC_ItemStack __instance)
        {
            if (__instance is XUiC_CombineAffixStack || __instance is XUiC_RamAffixStack)
                return false;

            return true;
        }
    }
}
