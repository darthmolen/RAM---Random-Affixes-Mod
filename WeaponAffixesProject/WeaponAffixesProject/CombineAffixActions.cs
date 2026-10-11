using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
internal static class CombineAffixActions
{
    internal static void TryCombine(XUiC_CombineGrid grid)
    {
        if (grid == null || grid.result == null || grid.result.IsEmpty())
            return;

        XUiC_CombineWindowGroup group = grid.GetParentByType<XUiC_CombineWindowGroup>();
        EntityPlayerLocal player = grid.xui?.playerUI?.entityPlayer;
        XUiM_PlayerInventory inventory = grid.xui?.PlayerInventory;
        if (group == null || player == null || inventory == null)
            return;

        if (!CombineAffixCost.CanTakeResult(grid))
            return;

        ItemStack result = grid.result.Clone();
        if (!inventory.AddItemNoPartial(result, true))
        {
            GameManager.ShowTooltip(player, Localization.Get("xuiInventoryFull"), string.Empty, "ui/ui_denied");
            return;
        }

        if (!CombineAffixCost.TryPayForResultPickup(grid))
            return;

        grid.merge1.ItemStack = ItemStack.Empty;
        grid.merge2.ItemStack = ItemStack.Empty;
        grid.SetResult(ItemStack.Empty);
        CombineAffixSelectionState.Clear(group);
    }
}
