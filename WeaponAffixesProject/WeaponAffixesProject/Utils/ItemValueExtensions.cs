namespace WeaponAffixesProject
{
    // V3.3 renamed ItemValue's affix arrays to cosmeticMods/modifications and added
    // SetCosmeticMod/SetModification, which call NotifyChanged() so the owning ItemStack
    // refreshes. Swapping a whole array has no such setter, so it writes the field and
    // notifies here. Without the notification the stack keeps rendering the old affixes.
    internal static class ItemValueExtensions
    {
        internal static void SetCosmeticMods(this ItemValue itemValue, ItemValue[] mods)
        {
            itemValue.cosmeticMods = mods;
            itemValue.NotifyChanged();
        }
    }
}
