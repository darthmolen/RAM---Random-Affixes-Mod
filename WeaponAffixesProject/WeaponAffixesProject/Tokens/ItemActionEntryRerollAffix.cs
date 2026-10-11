using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using WeaponAffixesProject;

[Preserve]
public class ItemActionEntryRerollAffix : BaseItemActionEntry
{
	private static float lastRerollTime = -999f;

	private const float CooldownSeconds = 1f;

	public ItemActionEntryRerollAffix(XUiController controller)
		: base(controller, "lblContextActionRerollAffix", "ui_game_symbol_reroll")
	{
	}

	public override void RefreshEnabled()
	{
		base.RefreshEnabled();
	}

	public override void OnActivated()
	{
		Log.Out("[Affix] Reroll button clicked");
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
		if (Time.time - lastRerollTime < 1f)
		{
			GameManager.ShowTooltip(player, string.Format(Localization.Get("ttWaitToReroll")), string.Empty, "ui_denied");
			return;
		}
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
		ItemClass requiredItem = ItemClass.GetItemClass("affixRerollToken");
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
		if (count > 0)
		{
			if (!RerollAffix(ref parentItemStack, affixMod.itemClass))
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
			lastRerollTime = Time.time;
			AffixUtils.ApplyAffixTokenUseEvents("affixRerollToken");
			LegendaryChallengeTracking.CheckMythicArmorChallenge(player);
			GameManager.ShowTooltip(player, string.Format(Localization.Get("ttRerollAffixSucces")), string.Empty, "recipe_unlocked");
		}
		else
		{
			Log.Out("Player does not have item: '" + requiredItem.Name + "'");
			cil?.AddItemStack(new ItemStack(requiredValue, 0));
			GameManager.ShowTooltip(player, string.Format(Localization.Get("ttRerollAffixRequiresItem"), requiredItem.localizedName), string.Empty);
		}
	}

	private static bool RerollAffix(ref ItemStack parentItem, ItemClass oldAffix)
	{
		ItemValue itemValue = parentItem.itemValue;
		int index = -1;
		List<List<ItemClassModifier>> modList = AffixUtils.GetCorrectModList(itemValue);
		if (modList == null || modList.Count == 0)
		{
			return false;
		}
		for (int i = 0; i < itemValue.cosmeticMods.Length; i++)
		{
			if (itemValue.cosmeticMods[i] != null && !itemValue.cosmeticMods[i].IsEmpty())
			{
				ItemClassModifier mod = itemValue.cosmeticMods[i].ItemClass as ItemClassModifier;
				modList = AffixUtils.RemoveSimilarMods(modList, mod);
				if (oldAffix.Name == mod.Name)
				{
					index = i;
				}
			}
		}
		if (!AffixUtils.TryGetAffixTierIndex(oldAffix.Name, out var oldTierIndex))
		{
			return false;
		}
		int oldTier = oldTierIndex + 1;
		int random = AffixUtils.rng.Next(0, 100);
		int targetTier = oldTier;
		if (oldTier > 1 && random < 35)
		{
			targetTier = oldTier - 1;
		}
		else if (oldTier < 6 && random < 60 && random >= 35)
		{
			targetTier = oldTier + 1;
		}
		if (targetTier > 6)
		{
			targetTier = 6;
		}
		if (!AffixUtils.TrySelectAffixMod(modList, targetTier - 1, out var selectedMod))
		{
			return false;
		}
		if (index < 0)
		{
			return false;
		}
		if (oldAffix.Name.Contains("KillStreak"))
		{
			GameManager.Instance.myEntityPlayerLocal.Buffs.RemoveBuff("buff" + oldAffix.Name);
		}
		itemValue.SetCosmeticMod(index, new ItemValue(selectedMod.Id));
		return true;
	}
}
