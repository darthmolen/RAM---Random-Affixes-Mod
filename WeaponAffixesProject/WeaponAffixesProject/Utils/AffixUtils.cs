using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Challenges;

namespace WeaponAffixesProject;

internal static class AffixUtils
{
	internal static readonly Random rng = new Random();

	internal static readonly FastTags<TagGroup.Global> AffixTag = FastTags<TagGroup.Global>.GetTag("affix_mod");

	internal static readonly FastTags<TagGroup.Global> UniqueAffixTag = FastTags<TagGroup.Global>.GetTag("unique_affix_mod");

	internal static readonly FastTags<TagGroup.Global> WeaponTag = FastTags<TagGroup.Global>.GetTag("weapon");

	internal static readonly FastTags<TagGroup.Global> ArmorTag = FastTags<TagGroup.Global>.GetTag("armor");

	internal static readonly FastTags<TagGroup.Global> ToolTag = FastTags<TagGroup.Global>.GetTag("tool");

	internal const int MaxNormallyObtainableAffixTier = 6;

	internal const int MaxRecognizedAffixTier = 7;

	internal static readonly int[][] baseWeightsByQuality = new int[7][]
	{
		null,
		new int[1] { 100 },
		new int[2] { 67, 33 },
		new int[3] { 50, 30, 20 },
		new int[4] { 33, 28, 22, 17 },
		new int[5] { 30, 25, 20, 15, 10 },
		new int[6] { 25, 23, 19, 15, 11, 7 }
	};

	private static readonly int[][] veryLowWeightsByQuality = new int[7][]
	{
		null,
		new int[1] { 100 },
		new int[2] { 85, 15 },
		new int[3] { 75, 20, 5 },
		new int[4] { 65, 23, 9, 3 },
		new int[5] { 58, 24, 12, 5, 1 },
		new int[6] { 52, 25, 14, 6, 2, 1 }
	};

	private static readonly int[][] lowWeightsByQuality = new int[7][]
	{
		null,
		new int[1] { 100 },
		new int[2] { 75, 25 },
		new int[3] { 60, 28, 12 },
		new int[4] { 45, 30, 17, 8 },
		new int[5] { 40, 28, 18, 10, 4 },
		new int[6] { 36, 28, 18, 11, 5, 2 }
	};

	private static readonly int[][] highWeightsByQuality = new int[7][]
	{
		null,
		new int[1] { 100 },
		new int[2] { 55, 45 },
		new int[3] { 38, 32, 30 },
		new int[4] { 24, 26, 25, 25 },
		new int[5] { 20, 22, 23, 20, 15 },
		new int[6] { 17, 18, 20, 20, 15, 10 }
	};

	private static readonly int[][] veryHighWeightsByQuality = new int[7][]
	{
		null,
		new int[1] { 100 },
		new int[2] { 42, 58 },
		new int[3] { 26, 31, 43 },
		new int[4] { 15, 20, 28, 37 },
		new int[5] { 12, 16, 22, 25, 25 },
		new int[6] { 10, 12, 16, 20, 22, 20 }
	};

	internal static readonly int requiredKills = 100;

	internal static readonly int unlockNewAffixChance = 67;

	internal static int RequiredKills => CustomSandboxSettings.GetInt("KillsToUpgrade", requiredKills);

	internal static int MaxAffixes => CustomSandboxSettings.GetInt("MaxAffixes", 5);

	internal static int AffixAbundance => CustomSandboxSettings.GetInt("AffixAbundance", 100);

	internal static int GetConfiguredMaxAffixes()
	{
		int configured = MaxAffixes;
		if (configured < 1)
		{
			return 1;
		}
		if (configured > 10)
		{
			return 10;
		}
		return configured;
	}

	internal static int GetNaturalMythicChallengeRequirement()
	{
		return GetNaturalMythicChallengeRequirement(GetConfiguredMaxAffixes());
	}

	internal static int GetNaturalMythicChallengeRequirement(int maxAffixes)
	{
		if (maxAffixes < 1)
		{
			return 1;
		}
		return (maxAffixes < 5) ? maxAffixes : 5;
	}

	internal static int GetEffectiveMaxAffixes(EntityPlayer player)
	{
		int maxAffixes = GetConfiguredMaxAffixes();
		if (!ChallengeGroupIsCompleted(player, "ram advanced"))
		{
			maxAffixes--;
		}
		if (GetProgressionLevel(player, "perkMagicSlayer") < 5)
		{
			maxAffixes--;
		}
		return (maxAffixes < 1) ? 1 : maxAffixes;
	}

	internal static int GetProgressionLevel(EntityPlayer player, string progressionName)
	{
		try
		{
			if (player?.Progression != null)
			{
				return player.Progression.GetProgressionValue(progressionName).level;
			}
		}
		catch (Exception arg)
		{
			Log.Out($"Can't find progression value '{progressionName}': '{arg}'");
		}
		return 0;
	}

	internal static int GetAdjustedKillsToUpgrade(int magicSlayerLevel)
	{
		int baseKills = RequiredKills;
		if (baseKills < 1)
		{
			baseKills = 1;
		}
		int reductionPercent = magicSlayerLevel * 5;
		if (reductionPercent < 0)
		{
			reductionPercent = 0;
		}
		if (reductionPercent > 25)
		{
			reductionPercent = 25;
		}
		int adjustedKills = (baseKills * (100 - reductionPercent) + 99) / 100;
		return (adjustedKills < 1) ? 1 : adjustedKills;
	}

	internal static bool IsAffixMod(ItemClass itemClass)
	{
		return itemClass != null && (IsAffixName(itemClass.Name) || itemClass.HasAnyTags(AffixTag) || itemClass.HasAnyTags(UniqueAffixTag) || IsAffixModifier(itemClass));
	}

	internal static bool IsGodlikeAffix(ItemClass itemClass)
	{
		int tierIndex;
		return IsAffixMod(itemClass) && TryGetAffixTierIndex(itemClass.Name, out tierIndex) && tierIndex + 1 == 7;
	}

	internal static int RandomizeTierWithOdds(ItemValue itemValue, EntityPlayer player)
	{
		int tier = itemValue?.Quality ?? 1;
		if (tier < 1)
		{
			tier = 1;
		}
		if (tier > 6)
		{
			tier = 6;
		}
		if (baseWeightsByQuality[tier] == null)
		{
			tier = 1;
		}
		int selectedTier = RollAffixTier(tier);
		int magicFindLvl = 0;
		if (player?.Progression == null)
		{
			Log.Out("Player progression not found");
		}
		else
		{
			magicFindLvl = player.Progression.GetProgressionValue("perkMagicFind").level;
		}
		bool upgraded = true;
		int maxTierIndex = ((magicFindLvl < 3) ? (tier - 1) : 5);
		while (selectedTier < maxTierIndex && upgraded)
		{
			int random = rng.Next(0, 100);
			if (random < magicFindLvl * 5)
			{
				Log.Out("Magic Find increased affix rarity!");
				selectedTier++;
			}
			else
			{
				upgraded = false;
			}
		}
		return selectedTier;
	}

	private static int RollAffixTier(int itemQuality)
	{
		int[] weights = GetRarityWeightsByQuality()[itemQuality];
		int totalWeight = 0;
		for (int i = 0; i < weights.Length; i++)
		{
			totalWeight += weights[i];
		}
		int roll = rng.Next(0, totalWeight);
		int cumulative = 0;
		for (int j = 0; j < weights.Length; j++)
		{
			cumulative += weights[j];
			if (roll < cumulative)
			{
				return j;
			}
		}
		return weights.Length - 1;
	}

	private static int[][] GetRarityWeightsByQuality()
	{
		return CustomSandboxSettings.GetInt("AffixRarity", 100) switch
		{
			25 => veryLowWeightsByQuality, 
			50 => lowWeightsByQuality, 
			150 => highWeightsByQuality, 
			200 => veryHighWeightsByQuality, 
			_ => baseWeightsByQuality, 
		};
	}

	internal static List<List<ItemClassModifier>> RemoveSimilarMods(List<List<ItemClassModifier>> modList, ItemClassModifier mod)
	{
		if (mod == null || string.IsNullOrEmpty(mod.Name))
		{
			return modList;
		}
		string baseName = GetAffixBaseName(mod.Name);
		for (int i = 0; i < modList.Count; i++)
		{
			for (int j = modList[i].Count - 1; j >= 0; j--)
			{
				if (modList[i][j].Name.StartsWith(baseName))
				{
					modList[i].Remove(modList[i][j]);
				}
			}
		}
		return modList;
	}

	internal static List<List<ItemClassModifier>> GetCorrectModList(ItemValue itemValue)
	{
		List<List<ItemClassModifier>> correctMods = new List<List<ItemClassModifier>>();
		for (int i = 0; i < 6; i++)
		{
			correctMods.Add(new List<ItemClassModifier>());
		}
		IEnumerable<ItemClassModifier> modList = GetAffixModifiers();
		foreach (ItemClassModifier itemClassModifier in modList)
		{
			if (TryGetAffixTierIndex(itemClassModifier.Name, out var tierIndex) && tierIndex < 6 && itemClassModifier.InstallableTags.Test_AnySet(itemValue.ItemClass.ItemTags) && !itemClassModifier.DisallowedTags.Test_AnySet(itemValue.ItemClass.ItemTags))
			{
				correctMods[tierIndex].Add(itemClassModifier);
			}
		}
		return correctMods;
	}

	internal static IEnumerable<ItemClassModifier> GetAffixModifiers()
	{
		if (ItemClass.list == null)
		{
			return Enumerable.Empty<ItemClassModifier>();
		}
		return from itemClassModifier in ItemClass.list.OfType<ItemClassModifier>()
			where itemClassModifier.Name.StartsWith("affixMod")
			select itemClassModifier;
	}

	internal static bool IsAffixModifier(ItemClass itemClass)
	{
		if (!(itemClass is ItemClassModifier itemClassModifier))
		{
			return false;
		}
		return IsAffixName(itemClassModifier.Name) || itemClassModifier.ModifierTags.Test_AnySet(AffixTag) || itemClassModifier.ItemTags.Test_AnySet(AffixTag) || itemClassModifier.ModifierTags.Test_AnySet(UniqueAffixTag) || itemClassModifier.ItemTags.Test_AnySet(UniqueAffixTag);
	}

	internal static bool IsAffixName(string itemClassName)
	{
		return !string.IsNullOrEmpty(itemClassName) && (itemClassName.StartsWith("affixMod", StringComparison.Ordinal) || itemClassName.StartsWith("uniqueAffixMod", StringComparison.Ordinal));
	}

	internal static bool IsAffixModWithKey(ItemClass itemClass, string affixKey)
	{
		if (!IsAffixMod(itemClass))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(affixKey) && itemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag(affixKey)))
		{
			return true;
		}
		string normalizedName = NormalizeAffixLookupText(itemClass.Name);
		string[] aliases = GetAffixNameAliases(affixKey);
		for (int i = 0; i < aliases.Length; i++)
		{
			if (normalizedName.Contains(aliases[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static string[] GetAffixNameAliases(string affixKey)
	{
		switch (NormalizeAffixLookupText(affixKey))
		{
		case "bulletcasing":
			return new string[1] { "bulletcasing" };
		case "giantslayer":
			return new string[1] { "giantslayer" };
		case "buffduration":
			return new string[1] { "buffduration" };
		case "bonuskill":
			return new string[2] { "bonuskill", "bonuskills" };
		case "bringdown":
			return new string[2] { "bringdown", "bringitdown" };
		case "permadeath":
			return new string[1] { "permadeath" };
		default:
		{
			string normalizedKey = NormalizeAffixLookupText(affixKey);
			return string.IsNullOrEmpty(normalizedKey) ? new string[0] : new string[1] { normalizedKey };
		}
		}
	}

	private static string NormalizeAffixLookupText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		char[] result = new char[text.Length];
		int count = 0;
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c))
			{
				result[count++] = char.ToLowerInvariant(c);
			}
		}
		return new string(result, 0, count);
	}

	internal static bool HasAnyMods(List<List<ItemClassModifier>> modList)
	{
		return modList?.Any((List<ItemClassModifier> tier) => tier != null && tier.Count > 0) ?? false;
	}

	internal static bool TrySelectAffixMod(List<List<ItemClassModifier>> modList, int preferredTier, out ItemClassModifier selectedMod)
	{
		selectedMod = null;
		if (!HasAnyMods(modList))
		{
			return false;
		}
		preferredTier = ClampTierIndex(preferredTier, modList.Count);
		for (int distance = 0; distance < modList.Count; distance++)
		{
			int lower = preferredTier - distance;
			if (TrySelectAffixFromTier(modList, lower, out selectedMod))
			{
				return true;
			}
			int upper = preferredTier + distance;
			if (upper != lower && TrySelectAffixFromTier(modList, upper, out selectedMod))
			{
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAffixTierIndex(string affixName, out int tierIndex)
	{
		tierIndex = -1;
		if (string.IsNullOrEmpty(affixName))
		{
			return false;
		}
		char tierChar = affixName[affixName.Length - 1];
		if (tierChar < '1' || tierChar > '7')
		{
			return false;
		}
		tierIndex = tierChar - 49;
		return true;
	}

	private static bool TrySelectAffixFromTier(List<List<ItemClassModifier>> modList, int tier, out ItemClassModifier selectedMod)
	{
		selectedMod = null;
		if (tier < 0 || tier >= modList.Count || modList[tier] == null || modList[tier].Count == 0)
		{
			return false;
		}
		selectedMod = modList[tier][rng.Next(modList[tier].Count)];
		return selectedMod != null;
	}

	private static int ClampTierIndex(int tierIndex, int tierCount)
	{
		if (tierIndex < 0)
		{
			return 0;
		}
		if (tierIndex >= tierCount)
		{
			return tierCount - 1;
		}
		return tierIndex;
	}

	private static string GetAffixBaseName(string affixName)
	{
		int tierIndex;
		return TryGetAffixTierIndex(affixName, out tierIndex) ? affixName.Substring(0, affixName.Length - 1) : affixName;
	}

	internal static bool IsCalledFromExtractAffix()
	{
		StackTrace trace = new StackTrace(fNeedFileInfo: false);
		StackFrame[] frames = trace.GetFrames();
		if (frames == null)
		{
			return false;
		}
		for (int i = 0; i < frames.Length; i++)
		{
			MethodBase method = frames[i].GetMethod();
			if ((method?.DeclaringType)?.Name == "ItemActionEntryExtractAffix" && method.Name == "OnActivated")
			{
				return true;
			}
		}
		return false;
	}

	internal static bool ChallengeGroupIsCompleted(EntityPlayer player, string groupName)
	{
		if (player?.challengeJournal?.CompleteChallengeGroupsForMinEvents == null)
		{
			return false;
		}
		foreach (ChallengeGroup group in player.challengeJournal.CompleteChallengeGroupsForMinEvents)
		{
			if (group.Name == groupName)
			{
				return true;
			}
		}
		return false;
	}

	internal static void ApplyQuestEventManagerUseItem(string name)
	{
		ItemClass requiredClass = ItemClass.GetItemClass(name);
		if (requiredClass != null)
		{
			ItemValue requiredValue = new ItemValue(requiredClass.Id);
			QuestEventManager quester = QuestEventManager.Current;
			if (quester != null)
			{
				quester.UsedItem(requiredValue);
			}
			else
			{
				Log.Out("Questmanager is null");
			}
		}
	}

	internal static void ApplyAffixTokenUseEvents(string tokenName, int amount = 1)
	{
		if (amount > 0)
		{
			for (int i = 0; i < amount; i++)
			{
				ApplyQuestEventManagerUseItem(tokenName);
				ApplyQuestEventManagerUseItem("affixModEntityDamagePerc7");
			}
		}
	}

	internal static int CountAffixesAtTier(ItemValue[] mods, int tier)
	{
		if (mods == null)
		{
			return 0;
		}
		int count = 0;
		foreach (ItemValue mod in mods)
		{
			if (mod?.ItemClass != null && !mod.IsEmpty() && IsAffixMod(mod.ItemClass) && TryGetAffixTierIndex(mod.ItemClass.Name, out var tierIndex) && tierIndex + 1 == tier)
			{
				count++;
			}
		}
		return count;
	}

	internal static List<int> GetAllAffixesFromItem(ItemValue item, string tagName)
	{
		List<int> instances = new List<int>();
		if (item == null)
		{
			return instances;
		}
		if (item.cosmeticMods != null)
		{
			ItemValue[] cosmeticMods = item.cosmeticMods;
			foreach (ItemValue mod in cosmeticMods)
			{
				if (mod != null && !mod.IsEmpty() && IsAffixModWithKey(mod.ItemClass, tagName) && TryGetAffixTierIndex(mod.ItemClass.Name, out var tierIndex))
				{
					instances.Add(tierIndex + 1);
				}
			}
		}
		if (item.modifications != null)
		{
			ItemValue[] modifications = item.modifications;
			foreach (ItemValue mod2 in modifications)
			{
				if (mod2 != null && !mod2.IsEmpty() && IsAffixModWithKey(mod2.ItemClass, tagName) && TryGetAffixTierIndex(mod2.ItemClass.Name, out var tierIndex2))
				{
					instances.Add(tierIndex2 + 1);
				}
			}
		}
		return instances;
	}

	// V3.3 removed Equipment.GetItems(); the slots are read one at a time now.
	internal static ItemValue[] GetEquippedItems(Equipment equipment)
	{
		if (equipment == null)
		{
			return null;
		}
		int slotCount = equipment.GetSlotCount();
		ItemValue[] items = new ItemValue[slotCount];
		for (int i = 0; i < slotCount; i++)
		{
			items[i] = equipment.GetSlotItem(i);
		}
		return items;
	}

	internal static List<int> GetAllAffixesFromArmor(EntityPlayer player, string tagName)
	{
		List<int> instances = new List<int>();
		ItemValue[] allGear = GetEquippedItems(player?.equipment);
		if (allGear == null)
		{
			return instances;
		}
		foreach (ItemValue gear in allGear)
		{
			instances.AddRange(GetAllAffixesFromItem(gear, tagName));
		}
		return instances;
	}
}
