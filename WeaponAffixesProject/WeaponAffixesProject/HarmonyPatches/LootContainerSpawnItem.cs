using System.Collections.Generic;
using HarmonyLib;

namespace WeaponAffixesProject;

[HarmonyPatch(typeof(LootContainer), "SpawnItem")]
public static class LootContainerSpawnItem
{
	// LootContainer.SpawnItem is static, so there is no instance to inject. Asking Harmony for
	// __instance on a static target cannot hand back a container — it is null at best and a
	// patch-time throw at worst — so the container-keyed de-duplication was never running.
	// AddGlobalTokenRoll already falls back to keying on the spawnedItems list when it has no
	// container, which is the behaviour this actually had.
	private static void Postfix(List<ItemStack> spawnedItems, EntityPlayer player)
	{
		LootContainerTokenAbundance.AddGlobalTokenRoll(null, spawnedItems);
		LootContainerTokenAbundance.ScaleTokenStacks(spawnedItems);
		AffixSystem.ApplyAffixToLootCheck(spawnedItems, player);
		LegendaryLootChallengeTracking.CheckNaturalMythicItems(spawnedItems);
	}
}
