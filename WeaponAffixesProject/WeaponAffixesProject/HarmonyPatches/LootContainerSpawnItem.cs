using System.Collections.Generic;
using HarmonyLib;

namespace WeaponAffixesProject;

[HarmonyPatch(typeof(LootContainer), "SpawnItem")]
public static class LootContainerSpawnItem
{
	// LootContainer.SpawnItem is static, so there is no __instance to inject. AddGlobalTokenRoll
	// falls back to de-duplicating on the spawnedItems list when it has no container.
	private static void Postfix(List<ItemStack> spawnedItems, EntityPlayer player)
	{
		LootContainerTokenAbundance.AddGlobalTokenRoll(null, spawnedItems);
		LootContainerTokenAbundance.ScaleTokenStacks(spawnedItems);
		AffixSystem.ApplyAffixToLootCheck(spawnedItems, player);
		LegendaryLootChallengeTracking.CheckNaturalMythicItems(spawnedItems);
	}
}
