using System.Collections.Generic;
using UnityEngine.Scripting;
using WeaponAffixesProject;

[Preserve]
public class ItemActionEntryExtractAffix : BaseItemActionEntry
{
	public ItemActionEntryExtractAffix(XUiController controller)
		: base(controller, "lblContextActionExtractAffix", "ui_game_symbol_extract")
	{
	}

	public override void RefreshEnabled()
	{
		base.Enabled = true;
	}

	public override void OnActivated()
	{
		if (WeaponAffixesProject.RamDebug.Enabled)
		    Log.Out("[Affix] Extract button clicked");
		if (!(base.ItemController is XUiC_BasePartStack affixMod) || !affixMod.ItemClass.Name.StartsWith("affixMod") || AffixUtils.IsGodlikeAffix(affixMod.ItemClass))
		{
			return;
		}
		EntityPlayerLocal player = GameManager.Instance.myEntityPlayerLocal;
		if (player == null)
		{
			return;
		}
		XUiM_PlayerInventory playerInventory = base.ItemController.xui.PlayerInventory;
		XUi xui = base.ItemController?.xui;
		if (xui == null)
		{
			return;
		}
		ItemStack parentItemStack = xui.AssembleItem?.CurrentItem;
		ItemValue parentItemValue = parentItemStack?.itemValue;
		if (parentItemValue == null || parentItemValue.IsEmpty())
		{
			return;
		}
		if (WeaponAffixesProject.RamDebug.Enabled)
		    Log.Out("Mod selected: '" + affixMod.ItemClass.localizedName + "'");
		if (WeaponAffixesProject.RamDebug.Enabled)
		    Log.Out("This mod is installed in: '" + parentItemValue.ItemClass.localizedName + "'");
		ItemClass requiredItem = ItemClass.GetItemClass("affixExtractionToken");
		if (requiredItem == null)
		{
			return;
		}
		int count = playerInventory.GetItemCount(new ItemValue(requiredItem.Id));
		ItemValue requiredValue = new ItemValue(requiredItem.Id);
		List<ItemStack> ingredients = new List<ItemStack>
		{
			new ItemStack(requiredValue, 1)
		};
		XUiC_CollectedItemList cil = base.ItemController?.xui?.CollectedItemList;
		if (!player.inventory.CanTakeItem(affixMod.itemStack) && !player.bag.CanTakeItem(affixMod.itemStack))
		{
			GameManager.ShowTooltip(player, string.Format(Localization.Get("xuiInventoryFullForPickup")), string.Empty, "ui_denied");
		}
		else if (count > 0)
		{
			if (!ExtractAffix(ref parentItemStack, affixMod))
			{
				return;
			}
			base.ItemController.xui.AssembleItem.currentItem = parentItemStack;
			base.ItemController.xui.AssembleItem.RefreshAssembleItem();
			XUiController assembleWg = xui.FindWindowGroupByName("assemble");
			if (assembleWg != null)
			{
				XUiC_ItemCosmeticStackGrid cosmeticGrid = assembleWg.GetChildByType<XUiC_ItemCosmeticStackGrid>();
				XUiC_AssembleWindow assembleWindow = assembleWg.GetChildByType<XUiC_AssembleWindow>();
				if (cosmeticGrid != null && assembleWindow != null)
				{
					cosmeticGrid.AssembleWindow = assembleWindow;
					cosmeticGrid.CurrentItem = parentItemStack;
					cosmeticGrid.SetParts();
					assembleWindow.ItemStack = parentItemStack;
					assembleWindow.OnChanged();
				}
			}
			playerInventory.RemoveItems(ingredients);
			cil?.RemoveItemStack(new ItemStack(requiredValue, 1));
			GameManager.ShowTooltip(player, string.Format(Localization.Get("ttExtractionAffixSucces")), string.Empty, "recipe_unlocked");
			AffixUtils.ApplyAffixTokenUseEvents("affixExtractionToken");
		}
		else
		{
			if (WeaponAffixesProject.RamDebug.Enabled)
			    Log.Out("Player does not have item: '" + requiredItem.Name + "'");
			cil?.AddItemStack(new ItemStack(requiredValue, 0));
			GameManager.ShowTooltip(player, string.Format(Localization.Get("ttExtractionAffixRequiresItem"), requiredItem.localizedName), string.Empty, "ui_denied");
		}
	}

	private bool ExtractAffix(ref ItemStack parentItem, XUiC_BasePartStack affix)
	{
		if (affix == null)
		{
			return false;
		}
		affix.HandleMoveToPreferredLocation();
		if (affix is XUiC_ItemCosmeticStack)
		{
			ItemValue[] newCosmeticModsList = new ItemValue[parentItem.itemValue.cosmeticMods.Length - 1];
			int j = 0;
			for (int i = 0; i < parentItem.itemValue.cosmeticMods.Length; i++)
			{
				if (parentItem.itemValue.cosmeticMods[i] != null && !parentItem.itemValue.cosmeticMods[i].IsEmpty())
				{
					newCosmeticModsList[j] = parentItem.itemValue.cosmeticMods[i];
					j++;
				}
			}
			parentItem.itemValue.SetCosmeticMods(newCosmeticModsList);
		}
		return true;
	}
}
