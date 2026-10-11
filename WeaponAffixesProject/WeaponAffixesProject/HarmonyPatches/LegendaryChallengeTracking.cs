using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace WeaponAffixesProject;

[HarmonyPatch]
internal static class LegendaryChallengeTracking
{
	private const int RequiredArmorPieces = 4;

	private const int RequiredMythicAffixes = 25;

	private const string MythicArmorChallengeEvent = "affixModEntityDamageBase7";

	private static IEnumerable<MethodBase> TargetMethods()
	{
		return from method in AccessTools.GetDeclaredMethods(typeof(Equipment))
			where method.Name == "SetSlot" || method.Name == "SetSlotItem" || method.Name == "SetSlotItemRaw"
			select method;
	}

	private static void Postfix(Equipment __instance)
	{
		EntityPlayerLocal player = GameManager.Instance?.myEntityPlayerLocal;
		if (player?.equipment == __instance)
		{
			CheckMythicArmorChallenge(player);
		}
	}

	internal static void CheckMythicArmorChallenge(EntityPlayerLocal player)
	{
		ItemValue[] equippedItems = AffixUtils.GetEquippedItems(player?.equipment);
		if (equippedItems == null)
		{
			return;
		}
		int armorPieces = 0;
		int mythicAffixes = 0;
		ItemValue[] array = equippedItems;
		foreach (ItemValue item in array)
		{
			if (item?.ItemClass != null && !item.IsEmpty() && item.ItemClass.HasAnyTags(AffixUtils.ArmorTag))
			{
				armorPieces++;
				mythicAffixes += CountMythicAffixes(item.cosmeticMods);
				mythicAffixes += CountMythicAffixes(item.modifications);
			}
		}
		if (armorPieces >= 4 && mythicAffixes >= 25)
		{
			AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase7");
		}
	}

	private static int CountMythicAffixes(ItemValue[] mods)
	{
		if (mods == null)
		{
			return 0;
		}
		int count = 0;
		foreach (ItemValue mod in mods)
		{
			if (mod?.ItemClass != null && !mod.IsEmpty() && AffixUtils.IsAffixMod(mod.ItemClass) && AffixUtils.TryGetAffixTierIndex(mod.ItemClass.Name, out var tierIndex) && tierIndex + 1 >= 6)
			{
				count++;
			}
		}
		return count;
	}
}
