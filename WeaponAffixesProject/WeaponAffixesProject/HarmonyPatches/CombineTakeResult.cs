using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches
{
    /// <summary>
    /// Charges the RAM affix-swap cost when the player takes a combine result.
    ///
    /// V3.2 reworked the combine station: the result is no longer a slot the player drags the
    /// item out of, it is a preview driven by bindings plus a Combine button. So the cost hook
    /// moved from the old <c>Result1_SlotChangedEvent</c> to <c>BtnCombine_OnPressed</c>, which
    /// is now the single path by which a result reaches the player.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_CombineGrid), nameof(XUiC_CombineGrid.BtnCombine_OnPressed))]
    public static class CombineTakeResultCost
    {
        private static bool Prefix(XUiC_CombineGrid __instance, out CombineAffixCost.PendingCost __state)
        {
            // Block the combine outright when the player cannot pay; TryReserve has already
            // shown them what they are missing.
            return CombineAffixCost.TryReserve(__instance, out __state);
        }

        private static void Postfix(XUiC_CombineGrid __instance, CombineAffixCost.PendingCost __state)
        {
            // Vanilla bails without clearing anything when the player's inventory is full, so
            // an empty result plus empty merge slots is what a completed combine looks like.
            // Only bill them for one that actually went through.
            if (!__instance.result.IsEmpty() || !__instance.merge1.ItemStack.IsEmpty())
                return;

            CombineAffixCost.Commit(__instance, __state);
            CombineAffixSelectionState.Clear(__instance.GetParentByType<XUiC_CombineWindowGroup>());
        }
    }
}
