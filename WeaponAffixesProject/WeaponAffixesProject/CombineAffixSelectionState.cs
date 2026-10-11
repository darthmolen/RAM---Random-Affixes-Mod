using System.Collections.Generic;

namespace WeaponAffixesProject;

internal static class CombineAffixSelectionState
{
	private const int MaxSlots = 10;

	private static readonly Dictionary<XUiC_CombineWindowGroup, bool[]> SelectedFromItemB = new Dictionary<XUiC_CombineWindowGroup, bool[]>();

	private static readonly Dictionary<XUiC_CombineWindowGroup, bool[]> HasExplicitSelection = new Dictionary<XUiC_CombineWindowGroup, bool[]>();

	internal static bool IsSelected(XUiC_CombineWindowGroup group, int slot, bool itemB)
	{
		if (group == null || slot < 0 || slot >= 10)
		{
			return false;
		}
		return GetArray(HasExplicitSelection, group)[slot] && GetArray(SelectedFromItemB, group)[slot] == itemB;
	}

	internal static void Toggle(XUiC_CombineWindowGroup group, int slot, bool itemB)
	{
		if (group != null && slot >= 0 && slot < 10)
		{
			bool[] explicitSelection = GetArray(HasExplicitSelection, group);
			bool[] selectedFromB = GetArray(SelectedFromItemB, group);
			if (explicitSelection[slot] && selectedFromB[slot] == itemB)
			{
				explicitSelection[slot] = false;
				selectedFromB[slot] = false;
			}
			else
			{
				explicitSelection[slot] = true;
				selectedFromB[slot] = itemB;
			}
		}
	}

	internal static void ClearIfSelected(XUiC_CombineWindowGroup group, int slot, bool itemB)
	{
		if (group != null && slot >= 0 && slot < 10)
		{
			bool[] explicitSelection = GetArray(HasExplicitSelection, group);
			bool[] selectedFromB = GetArray(SelectedFromItemB, group);
			if (explicitSelection[slot] && selectedFromB[slot] == itemB)
			{
				explicitSelection[slot] = false;
				selectedFromB[slot] = false;
			}
		}
	}

	internal static void Clear(XUiC_CombineWindowGroup group)
	{
		if (group != null)
		{
			HasExplicitSelection.Remove(group);
			SelectedFromItemB.Remove(group);
		}
	}

	internal static ItemValue[] BuildResultAffixes(XUiC_CombineWindowGroup group, ItemValue itemA, ItemValue itemB)
	{
		int maxAffixes = AffixUtils.GetConfiguredMaxAffixes();
		List<ItemValue> result = new List<ItemValue>(maxAffixes);
		bool[] explicitSelection = GetArray(HasExplicitSelection, group);
		bool[] selectedFromB = GetArray(SelectedFromItemB, group);
		for (int i = 0; i < maxAffixes; i++)
		{
			ItemValue[] sourceMods = ((!explicitSelection[i] || !selectedFromB[i] || IsGodlikeAffixAt(itemB?.cosmeticMods, i)) ? itemA?.cosmeticMods : itemB?.cosmeticMods);
			ItemValue affix = CloneAffixAt(sourceMods, i);
			if (!affix.IsEmpty())
			{
				result.Add(affix);
			}
		}
		return result.ToArray();
	}

	internal static int CountSelectedAffixesFromItemB(XUiC_CombineWindowGroup group, ItemValue itemB)
	{
		return CountSelectedAffixesFromItemB(group, itemB, null);
	}

	internal static int CountSelectedAffixesFromItemBAtTier(XUiC_CombineWindowGroup group, ItemValue itemB, int tier)
	{
		return CountSelectedAffixesFromItemB(group, itemB, tier);
	}

	private static int CountSelectedAffixesFromItemB(XUiC_CombineWindowGroup group, ItemValue itemB, int? requiredTier)
	{
		if (group == null || itemB?.cosmeticMods == null)
		{
			return 0;
		}
		int maxAffixes = AffixUtils.GetConfiguredMaxAffixes();
		bool[] explicitSelection = GetArray(HasExplicitSelection, group);
		bool[] selectedFromB = GetArray(SelectedFromItemB, group);
		int count = 0;
		for (int i = 0; i < maxAffixes; i++)
		{
			if (explicitSelection[i] && selectedFromB[i] && i < itemB.cosmeticMods.Length && itemB.cosmeticMods[i] != null && !itemB.cosmeticMods[i].IsEmpty() && !IsGodlikeAffixAt(itemB.cosmeticMods, i) && (!requiredTier.HasValue || (AffixUtils.TryGetAffixTierIndex(itemB.cosmeticMods[i].ItemClass?.Name, out var tierIndex) && tierIndex + 1 == requiredTier.Value)))
			{
				count++;
			}
		}
		return count;
	}

	private static ItemValue CloneAffixAt(ItemValue[] mods, int slot)
	{
		if (mods == null || slot < 0 || slot >= mods.Length || mods[slot] == null || mods[slot].IsEmpty())
		{
			return ItemValue.None.Clone();
		}
		return mods[slot].Clone();
	}

	internal static bool IsGodlikeAffixAt(ItemValue[] mods, int slot)
	{
		if (mods == null || slot < 0 || slot >= mods.Length || mods[slot] == null || mods[slot].IsEmpty())
		{
			return false;
		}
		return IsGodlikeAffix(mods[slot]);
	}

	internal static bool IsGodlikeAffix(ItemValue affix)
	{
		int tierIndex;
		return affix != null && !affix.IsEmpty() && AffixUtils.TryGetAffixTierIndex(affix.ItemClass?.Name, out tierIndex) && tierIndex + 1 == 7;
	}

	private static bool[] GetArray(Dictionary<XUiC_CombineWindowGroup, bool[]> states, XUiC_CombineWindowGroup group)
	{
		if (!states.TryGetValue(group, out var state))
		{
			state = (states[group] = new bool[10]);
		}
		return state;
	}
}
