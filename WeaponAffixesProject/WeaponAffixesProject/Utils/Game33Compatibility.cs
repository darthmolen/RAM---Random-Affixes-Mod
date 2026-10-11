namespace WeaponAffixesProject;

internal static class Game33Compatibility
{
    internal static void SetCosmeticMods(this ItemValue item, ItemValue[] mods)
    {
        item.cosmeticMods = mods;
        item.NotifyChanged();
    }

    internal static ItemValue[] GetItems(this Equipment equipment)
    {
        var items = new ItemValue[equipment.GetSlotCount()];
        for (int i = 0; i < items.Length; i++)
            items[i] = equipment.GetSlotItem(i);
        return items;
    }

    internal static void SetSlots(this XUiC_PartList list, ItemValue[] mods, int offset)
    {
        int capacity = list.GetItemStackControllers().Length;
        for (int i = 0; i + offset < capacity; i++)
            list.SetPart(mods != null && i < mods.Length ? mods[i] : null, i + offset);
    }
}
