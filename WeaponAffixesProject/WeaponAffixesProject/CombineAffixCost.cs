using System.Collections.Generic;
using System.Text;

namespace WeaponAffixesProject;

internal static class CombineAffixCost
{
	private const string SwapTokenName = "affixSwapToken";

	private const string TransferMythicChallengeEvent = "affixModBonusKills7";

	/// <summary>
	/// A cost worked out from the grid before the combine runs, so it can be charged
	/// afterwards once the result has actually reached the player's inventory.
	/// </summary>
	internal sealed class PendingCost
	{
		internal List<ItemStack> Costs;

		internal int ImportedAffixes;

		internal int ImportedMythicAffixes;
	}

	/// <summary>
	/// Works out what taking the current result would cost and checks the player can pay it.
	/// Returns false (after showing a tooltip) when they cannot, in which case the combine
	/// must be blocked. <paramref name="pending" /> is null when nothing was imported from
	/// item B and the combine is therefore free.
	/// </summary>
	internal static bool TryReserve(XUiC_CombineGrid grid, out PendingCost pending)
	{
		pending = null;
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
		pending = new PendingCost
		{
			Costs = costs,
			ImportedAffixes = importedAffixes,
			ImportedMythicAffixes = importedMythicAffixes
		};
		return true;
	}

	/// <summary>
	/// Charges a cost previously reserved by <see cref="TryReserve" />. Only call this once
	/// the combine has actually completed, so a failed combine never bills the player.
	/// </summary>
	internal static void Commit(XUiC_CombineGrid grid, PendingCost pending)
	{
		if (grid == null || pending == null || pending.Costs == null)
		{
			return;
		}
		XUi xui = grid.xui;
		XUiM_PlayerInventory inventory = xui?.PlayerInventory;
		if (inventory == null)
		{
			return;
		}
		inventory.RemoveItems(pending.Costs);
		foreach (ItemStack cost in pending.Costs)
		{
			xui.CollectedItemList?.RemoveItemStack(cost);
		}
		AffixUtils.ApplyAffixTokenUseEvents("affixSwapToken", pending.ImportedAffixes);
		for (int i = 0; i < pending.ImportedMythicAffixes; i++)
		{
			AffixUtils.ApplyQuestEventManagerUseItem("affixModBonusKills7");
		}
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
