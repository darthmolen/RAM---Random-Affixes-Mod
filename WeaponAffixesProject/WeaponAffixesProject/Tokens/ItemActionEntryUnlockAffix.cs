using System.Collections.Generic;
using WeaponAffixesProject;

public class ItemActionEntryUnlockAffix : BaseItemActionEntry
{
	private const string UnlockMythicChallengeEvent = "affixModBringItDown7";

	public ItemActionEntryUnlockAffix(XUiController controller)
		: base(controller, "lblContextActionUnlockAffix", "ui_game_symbol_unlock2")
	{
	}

	public override void RefreshEnabled()
	{
		base.Enabled = true;
	}

	public override void OnActivated()
	{
		Log.Out("[Affix] Unlock button clicked");
		if (!(base.ItemController is XUiC_ItemStack item) || (!item.itemClass.HasAnyTags(AffixUtils.ArmorTag) && !item.itemClass.HasAnyTags(AffixUtils.WeaponTag) && !item.itemClass.HasAnyTags(AffixUtils.ToolTag)))
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
		string materialName = item.itemClass.MadeOfMaterial.GetLocalizedMaterialName();
		ItemClass requiredItem = ItemClass.GetItemClass("affixUnlockToken");
		ItemClass requiredItem2 = GetItemFromMaterial(materialName);
		Log.Out(materialName);
		if (requiredItem == null)
		{
			return;
		}
		int count = playerInventory.GetItemCount(new ItemValue(requiredItem.Id));
		int count2 = ((requiredItem2 == null) ? 10 : playerInventory.GetItemCount(new ItemValue(requiredItem2.Id)));
		ItemValue requiredValue2 = null;
		if (requiredItem2 != null)
		{
			requiredValue2 = new ItemValue(requiredItem2.Id);
		}
		ItemValue requiredValue3 = new ItemValue(requiredItem.Id);
		List<ItemStack> ingredients = ((requiredValue2 != null) ? new List<ItemStack>
		{
			new ItemStack(requiredValue3, 1),
			new ItemStack(requiredValue2, 10)
		} : new List<ItemStack>
		{
			new ItemStack(requiredValue3, 1)
		});
		XUiC_CollectedItemList cil = base.ItemController?.xui?.CollectedItemList;
		if (count > 0 && count2 > 9)
		{
			string newAffix = "";
			int mythicAffixesBefore = AffixUtils.CountAffixesAtTier(item.itemStack.itemValue.cosmeticMods, 6);
			if (UnlockAffix(ref item.itemStack, ref newAffix))
			{
				bool unlockedMythic = AffixUtils.CountAffixesAtTier(item.itemStack.itemValue.cosmeticMods, 6) > mythicAffixesBefore;
				item.ForceRefreshItemStack();
				XUiController assembleWg = xui.FindWindowGroupByName("assemble");
				if (assembleWg != null)
				{
					XUiC_ItemCosmeticStackGrid cosmeticGrid = assembleWg.GetChildByType<XUiC_ItemCosmeticStackGrid>();
					XUiC_AssembleWindow assembleWindow = assembleWg.GetChildByType<XUiC_AssembleWindow>();
					if (cosmeticGrid != null && assembleWindow != null)
					{
						cosmeticGrid.AssembleWindow = assembleWindow;
						cosmeticGrid.CurrentItem = item.itemStack;
						cosmeticGrid.SetParts();
						assembleWindow.ItemStack = item.itemStack;
						assembleWindow.OnChanged();
					}
				}
				playerInventory.RemoveItems(ingredients);
				foreach (ItemStack ingredient in ingredients)
				{
					Log.Out(ingredient.itemValue.ItemClass.localizedName);
					cil?.RemoveItemStack(ingredient);
				}
				AffixUtils.ApplyAffixTokenUseEvents("affixUnlockToken");
				if (unlockedMythic)
				{
					AffixUtils.ApplyQuestEventManagerUseItem("affixModBringItDown7");
				}
				LegendaryChallengeTracking.CheckMythicArmorChallenge(player);
				GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixSucces"), newAffix), string.Empty, "recipe_unlocked");
			}
			else
			{
				GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixFail"), AffixUtils.GetEffectiveMaxAffixes(player)), string.Empty, "ui/ui_denied");
			}
		}
		else
		{
			if (count < 1 && count2 < 10)
			{
				cil?.AddItemStack(new ItemStack(requiredValue3, 0));
				cil?.AddItemStack(new ItemStack(requiredValue2, 0));
			}
			else if (count < 1 && count2 >= 10)
			{
				cil?.AddItemStack(new ItemStack(requiredValue3, 0));
			}
			else if (count >= 1 && count2 < 10)
			{
				cil?.AddItemStack(new ItemStack(requiredValue2, 0));
			}
			if (requiredItem2 != null)
			{
				GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixRequiresItem"), requiredItem.localizedName, requiredItem2.localizedName), string.Empty, "ui/ui_denied");
			}
			else
			{
				GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUnlockAffixRequiresItem2"), requiredItem.localizedName), string.Empty, "ui/ui_denied");
			}
		}
	}

	public static bool UnlockAffix(ref ItemStack itemStack, ref string newAffix)
	{
		EntityPlayerLocal player = GameManager.Instance?.myEntityPlayerLocal;
		ItemValue[] cosmeticMods = itemStack.itemValue.cosmeticMods;
		int currentSlots = ((cosmeticMods != null) ? cosmeticMods.Length : 0);
		if (currentSlots >= AffixUtils.GetEffectiveMaxAffixes(player))
		{
			return false;
		}
		return AffixSystem.AddNewAffix(itemStack.itemValue, ref newAffix, fromToken: true, player);
	}

	public static ItemClass GetItemFromMaterial(string materialName)
	{
		if (!materialName.Contains("Parts") && !materialName.Contains("armor") && !materialName.Contains("MmeleeToolAllSteel"))
		{
			return null;
		}
		if (materialName.Contains("armor"))
		{
			return ItemClass.GetItemClass("armorParts", _caseInsensitive: true);
		}
		if (materialName.Contains("MmeleeToolAllSteel"))
		{
			return ItemClass.GetItemClass("meleeToolAllSteelParts", _caseInsensitive: true);
		}
		List<ItemClass> items = ItemClass.GetItemsWithTag(FastTags<TagGroup.Global>.GetTag("parts"));
		foreach (ItemClass item in items)
		{
			if (item.MadeOfMaterial.GetLocalizedMaterialName() == materialName)
			{
				return item;
			}
		}
		return null;
	}
}
