using System.Collections.Generic;
using System.Text;

namespace WeaponAffixesProject;

internal static class CombineAffixCost
{
	private const string SwapTokenName = "affixSwapToken";

	private const string TransferMythicChallengeEvent = "affixModBonusKills7";

	internal static bool CanTakeResult(XUiC_CombineGrid grid)
	{
		return CheckResultCosts(grid, payCosts: false);
	}

	internal static bool TryPayForResultPickup(XUiC_CombineGrid grid)
	{
		return CheckResultCosts(grid, payCosts: true);
	}

	private static bool CheckResultCosts(XUiC_CombineGrid grid, bool payCosts)
	{
		if (grid == null)
		{
			return true;
		}
		ItemStack result = grid.result;
		if (result == null || result.IsEmpty())
		{
			return true;
		}
		XUiC_CombineWindowGroup group = grid.GetParentByType<XUiC_CombineWindowGroup>();
		int importedAffixes = CombineAffixSelectionState.CountSelectedAffixesFromItemB(group, grid.merge2.ItemStack?.itemValue);
		int importedMythicAffixes = CombineAffixSelectionState.CountSelectedAffixesFromItemBAtTier(group, grid.merge2.ItemStack?.itemValue, 6);
		if (importedAffixes <= 0)
		{
			return true;
		}
		XUi xui = grid.xui;
		EntityPlayerLocal player = xui?.playerUI?.entityPlayer;
		XUiM_PlayerInventory inventory = xui?.PlayerInventory;
		if (player == null || inventory == null)
		{
			return false;
		}
		List<ItemStack> costs = BuildCosts(grid.merge1.ItemStack?.itemValue, importedAffixes);
		List<ItemStack> missing = GetMissingCosts(inventory, costs);
		if (missing.Count > 0)
		{
			ShowMissingCost(player, xui, missing);
			return false;
		}
		if (!payCosts)
		{
			return true;
		}
		inventory.RemoveItems(costs);
		foreach (ItemStack cost in costs)
		{
			xui.CollectedItemList?.RemoveItemStack(cost);
		}
		AffixUtils.ApplyAffixTokenUseEvents("affixSwapToken", importedAffixes);
		for (int i = 0; i < importedMythicAffixes; i++)
		{
			AffixUtils.ApplyQuestEventManagerUseItem("affixModBonusKills7");
		}
		return true;
	}

	private static List<ItemStack> BuildCosts(ItemValue itemValue, int importedAffixes)
	{
		List<ItemStack> costs = new List<ItemStack>();
		ItemClass swapToken = ItemClass.GetItemClass("affixSwapToken");
		if (swapToken != null)
		{
			costs.Add(new ItemStack(new ItemValue(swapToken.Id), importedAffixes));
		}
		ItemClass parts = GetPartsItem(itemValue);
		if (parts != null)
		{
			costs.Add(new ItemStack(new ItemValue(parts.Id), importedAffixes * 5));
		}
		return costs;
	}

	private static ItemClass GetPartsItem(ItemValue itemValue)
	{
		string materialName = (itemValue?.ItemClass)?.MadeOfMaterial?.GetLocalizedMaterialName();
		return string.IsNullOrEmpty(materialName) ? null : ItemActionEntryUnlockAffix.GetItemFromMaterial(materialName);
	}

	private static List<ItemStack> GetMissingCosts(XUiM_PlayerInventory inventory, List<ItemStack> costs)
	{
		List<ItemStack> missing = new List<ItemStack>();
		foreach (ItemStack cost in costs)
		{
			int owned = inventory.GetItemCount(cost.itemValue);
			if (owned < cost.count)
			{
				missing.Add(cost);
			}
		}
		return missing;
	}

	private static void ShowMissingCost(EntityPlayerLocal player, XUi xui, List<ItemStack> missing)
	{
		StringBuilder costList = new StringBuilder();
		for (int i = 0; i < missing.Count; i++)
		{
			ItemStack cost = missing[i];
			if (i > 0)
			{
				costList.Append(Localization.Get(i == missing.Count - 1 ? "ramListSeparatorFinal" : "ramListSeparator"));
			}
			costList.Append(string.Format(Localization.Get("ramCombineMissingCostEntry"), cost.count, cost.itemValue.ItemClass.localizedName));
			xui.CollectedItemList?.AddItemStack(new ItemStack(cost.itemValue, 0));
		}
		string messageKey = missing.Count == 1 && missing[0].count == 1
			? "ramCombineMissingCostSingle"
			: "ramCombineMissingCostPlural";
		string message = string.Format(Localization.Get(messageKey), costList);
		GameManager.ShowTooltip(player, message.ToString(), string.Empty, "ui/ui_denied");
	}
}
