using System.Collections.Generic;

namespace WeaponAffixesProject;

internal static class LegendaryLootChallengeTracking
{
	private const string NaturalMythicChallengeEvent = "affixModLevelDamage7";

	internal static void CheckNaturalMythicItems(List<ItemStack> spawnedItems)
	{
		if (spawnedItems == null)
		{
			return;
		}
		int requiredMythics = AffixUtils.GetNaturalMythicChallengeRequirement();
		foreach (ItemStack spawnedItem in spawnedItems)
		{
			ItemValue item = spawnedItem?.itemValue;
			if (item?.ItemClass != null && !item.IsEmpty() && (item.ItemClass.HasAnyTags(AffixUtils.WeaponTag) || item.ItemClass.HasAnyTags(AffixUtils.ToolTag) || item.ItemClass.HasAnyTags(AffixUtils.ArmorTag)))
			{
				int mythicAffixes = AffixUtils.CountAffixesAtTier(item.cosmeticMods, 6);
				if (mythicAffixes >= requiredMythics)
				{
					AffixUtils.ApplyQuestEventManagerUseItem("affixModLevelDamage7");
					break;
				}
			}
		}
	}
}
