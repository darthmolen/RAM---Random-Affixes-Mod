using System;
using HarmonyLib;
using WeaponBuffMod.HarmonyPatches;

namespace WeaponAffixesProject.HarmonyPatches;

internal static class RamLocalizationTokens
{
	internal static string Replace(string text)
	{
		if (string.IsNullOrEmpty(text) || text.IndexOf("{ram.") < 0)
		{
			return text;
		}
		int fullCap = RamSandboxOptions.GetMaxAffixesValue();
		int baseCap = ClampAffixCap(fullCap - 2);
		int challengeCap = ClampAffixCap(fullCap - 1);
		int unlockChance = 100 - AffixUtils.unlockNewAffixChance;
		return text.Replace("{ram.maxAffixes}", fullCap.ToString()).Replace("{ram.baseAffixCap}", baseCap.ToString()).Replace("{ram.challengeAffixCap}", challengeCap.ToString())
			.Replace("{ram.naturalMythicRequirement}", AffixUtils.GetNaturalMythicChallengeRequirement(fullCap).ToString())
			.Replace("{ram.killsToUpgrade}", RamSandboxOptions.GetKillsToUpgradeValue().ToString())
			.Replace("{ram.affixAbundance}", RamSandboxOptions.GetAffixAbundanceValue().ToString())
			.Replace("{ram.unlockAffixChance}", unlockChance.ToString());
	}

	private static int ClampAffixCap(int value)
	{
		return (value < 1) ? 1 : value;
	}
}

[HarmonyPatch(typeof(Localization), nameof(Localization.Get), new Type[] { typeof(string), typeof(bool), typeof(string) })]
internal static class LocalizationGetRamTokens
{
	private static void Postfix(ref string __result)
	{
		__result = RamLocalizationTokens.Replace(__result);
	}
}

[HarmonyPatch(typeof(Localization), nameof(Localization.TryGet))]
internal static class LocalizationTryGetRamTokens
{
	private static void Postfix(ref string _localizedString)
	{
		_localizedString = RamLocalizationTokens.Replace(_localizedString);
	}
}
