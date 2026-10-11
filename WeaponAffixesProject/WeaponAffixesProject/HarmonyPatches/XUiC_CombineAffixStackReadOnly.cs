using HarmonyLib;
using UnityEngine;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemStack))]
    public static class XUiC_CombineAffixStackReadOnly
    {
        private static readonly Color32 SelectedAffixBorder = new Color32(222, 206, 163, 255);

        /// <summary>
        /// Shows which affix slots are taken from item B.
        ///
        /// Vanilla's templates bind the stack background to {selectionbordercolor}. Up to V3.2
        /// XUiC_CombineAffixStack answered that binding directly, but V3.3 added
        /// XUiC_ItemStack.bindingSelectionBorderColor(), which XUi resolves ahead of
        /// GetBindingValueInternal, so the override stopped being consulted and the highlight
        /// silently disappeared. Selecting an affix still worked — there was just no way to see
        /// that it had, which reads as a dead button.
        ///
        /// updateBorderColor() rewrites the property from vanilla's own rules every frame, so
        /// this runs after it and only when the value actually differs. IsDirty is consumed by
        /// the IsDirty check later in the same Update, which refreshes the binding; setting it
        /// unconditionally would refresh bindings every frame and restart the hover tween.
        /// </summary>
        [HarmonyPatch("updateBorderColor")]
        [HarmonyPostfix]
        public static void UpdateBorderColorPostfix(XUiC_ItemStack __instance)
        {
            if (!(__instance is XUiC_CombineAffixStack affixStack) || !affixStack.IsAffixSelected)
                return;

            Color32 current = __instance.SelectionBorderColor;
            if (current.r == SelectedAffixBorder.r && current.g == SelectedAffixBorder.g &&
                current.b == SelectedAffixBorder.b && current.a == SelectedAffixBorder.a)
                return;

            __instance.SelectionBorderColor = SelectedAffixBorder;
            __instance.IsDirty = true;
        }

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
