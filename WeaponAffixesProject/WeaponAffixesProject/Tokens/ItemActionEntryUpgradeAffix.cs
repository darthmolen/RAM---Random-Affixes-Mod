using System;
using System.Collections.Generic;

namespace WeaponAffixesProject;

internal class ItemActionEntryUpgradeAffix : BaseItemActionEntry
{
	public ItemActionEntryUpgradeAffix(XUiController controller)
		: base(controller, "lblContextActionUpgradeAffix", "ui_game_symbol_upgrade")
	{
	}

	public override void RefreshEnabled()
	{
		base.Enabled = true;
	}

	public override void OnActivated()
	{
		Log.Out("[Affix] Upgrade button clicked");
		if (!(base.ItemController is XUiC_BasePartStack affixMod) || !affixMod.ItemClass.Name.StartsWith("affixMod"))
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
		Log.Out("Mod selected: '" + affixMod.ItemClass.localizedName + "'");
		Log.Out("This mod is installed in: '" + parentItemValue.ItemClass.localizedName + "'");
		ItemClass requiredItem = ItemClass.GetItemClass("affixUpgradeToken");
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
		if (count <= 0)
		{
			Log.Out("Player does not have item: '" + requiredItem.Name + "'");
			cil?.AddItemStack(new ItemStack(requiredValue, 0));
			GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUpgradeAffixRequiresItem"), requiredItem.localizedName), string.Empty, "ui_denied");
			return;
		}
		string affixName = affixMod.itemClass.Name;
		char lastChar = affixName[affixName.Length - 1];
		if (char.IsDigit(lastChar))
		{
			int value = lastChar - 48;
			if (value < 6)
			{
				List<int> upgradeSlot = new List<int>();
				for (int i = 0; i < parentItemValue.cosmeticMods.Length; i++)
				{
					if (parentItemValue.cosmeticMods[i].ItemClass.Name == affixName)
					{
						upgradeSlot.Add(i);
						break;
					}
				}
				if (AffixSystem.UpgradeAffix(parentItemValue, upgradeSlot, ref affixName))
				{
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
					if (parentItemValue.TryGetMetadata("upgrades", out float upgrades) && parentItemValue.TryGetMetadata("nextUpgrade", out float _) && parentItemValue.TryGetMetadata("lastUpgrade", out float lastUpgrade))
					{
						int magicSlayerLvl = 0;
						try
						{
							magicSlayerLvl = player.Progression.GetProgressionValue("perkMagicSlayer").level;
						}
						catch (Exception arg)
						{
							Log.Out($"Can't find magic slayer perk: '{arg}'");
						}
						int killsToUpgrade = AffixUtils.GetAdjustedKillsToUpgrade(magicSlayerLvl);
						WeaponUpgrades.RecordUpgrade(parentItemStack.itemValue, upgrades);
						parentItemStack.itemValue.SetMetadata("nextUpgrade", lastUpgrade + (float)killsToUpgrade * (upgrades + 2f));
						base.ItemController.xui.AssembleItem.currentItem = parentItemStack;
						base.ItemController.xui.AssembleItem.RefreshAssembleItem();
					}
					playerInventory.RemoveItems(ingredients);
					cil?.RemoveItemStack(new ItemStack(requiredValue, 1));
					GameManager.ShowTooltip(player, string.Format(Localization.Get("ttUpgradeAffixSucces")), string.Empty, "recipe_unlocked");
					AffixUtils.ApplyAffixTokenUseEvents("affixUpgradeToken");
					LegendaryChallengeTracking.CheckMythicArmorChallenge(player);
					return;
				}
			}
		}
		GameManager.ShowTooltip(player, string.Format(Localization.Get("ttFailedToUpgrade")), string.Empty, "ui_denied");
	}
}
