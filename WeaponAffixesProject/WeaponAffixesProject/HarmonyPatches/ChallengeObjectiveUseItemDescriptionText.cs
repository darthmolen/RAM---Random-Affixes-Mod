using Challenges;
using HarmonyLib;

namespace WeaponAffixesProject;

[HarmonyPatch(typeof(ChallengeObjectiveUseItem), "get_DescriptionText")]
public static class ChallengeObjectiveUseItemDescriptionText
{
	public static void Postfix(ChallengeObjectiveUseItem __instance, ref string __result)
	{
		switch (__instance.Owner?.ChallengeClass?.Name)
		{
		case "getoneaffix":
			__result = "Kill With Any Affix";
			break;
		case "upgradeoneaffix":
			__result = "Upgrade Any Affix";
			break;
		case "getoneaffixrare":
			__result = "Kill With Any Rare Affix";
			break;
		case "unlockoneaffix":
			__result = "Unlock Any Affix";
			break;
		case "getthreeaffixes":
			__result = "Kill With 3 Affixes";
			break;
		case "upgradeepicaffix":
			__result = "Upgrade to Epic Affix";
			break;
		case "upgrademythicaffix":
			__result = "Upgrade to Mythic Affix";
			break;
		case "getfiveaffixes":
			__result = "Kill With 6 Affixes";
			break;
		case "upgradefifteenaffixes":
			__result = "Upgrade Any Affix";
			break;
		case "upgradefifteentimes":
			__result = "Upgrade 1 Weapon 15 times";
			break;
		case "upgradetenmythicaffix":
			__result = "Upgrade to Mythic Affix";
			break;
		case "getelevenaffixes":
			__result = "Kill With 11 Affixes";
			break;
		case "getfivemythicaffixes":
			__result = "Kill with 5+ Mythic Affixes";
			break;
		case "spendhundredtokens":
			__result = "Spend Any Affix Token";
			break;
		case "weartwentyfivemythicaffixes":
			__result = "Wear 25+ Mythic Affixes";
			break;
		case "ascendweapon":
			__result = "Ascend Any Weapon";
			break;
		case "ascendweaponthreetimes":
			__result = "Ascend One Weapon 3 Times";
			break;
		case "transferfivemythicaffixes":
			__result = "Transfer Mythic Affix";
			break;
		case "unlockmythicwithtoken":
			__result = "Unlock a Mythic Affix";
			break;
		case "findnaturalmythicaffixes":
			__result = "Find " + AffixUtils.GetNaturalMythicChallengeRequirement() + " Natural Mythic Affixes";
			break;
		}
	}
}
