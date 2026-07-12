using UnityEngine;
using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public class XUiC_RamAffixDetails : XUiController
{
    private ItemStack selectedAffix = ItemStack.Empty;

    internal ItemStack SelectedAffix => selectedAffix ?? ItemStack.Empty;

    internal void SetSelectedAffix(ItemStack affixStack)
    {
        selectedAffix = affixStack == null || affixStack.IsEmpty() ? ItemStack.Empty : affixStack.Clone();
        RefreshBindings();
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        ItemValue itemValue = selectedAffix?.itemValue;
        ItemClass itemClass = itemValue?.ItemClass;

        switch (bindingName)
        {
            case "ramdetailhasaffix":
                value = (itemClass != null && !selectedAffix.IsEmpty()).ToString().ToLowerInvariant();
                return true;
            case "ramdetailnoaffix":
                value = (itemClass == null || selectedAffix.IsEmpty()).ToString().ToLowerInvariant();
                return true;
            case "ramdetailname":
                value = itemClass?.GetLocalizedItemName() ?? string.Empty;
                return true;
            case "ramdetaildescription":
                value = GetDescription(itemClass);
                return true;
            case "ramdetailicon":
                value = itemValue?.GetPropertyOverride("CustomIcon", itemClass?.GetIconName() ?? string.Empty) ?? string.Empty;
                return true;
            case "ramdetailicontint":
                value = itemClass == null ? "255,255,255,255" : FormatColor(itemClass.GetIconTint(itemValue));
                return true;
            case "ramdetailtier":
                value = GetTierText(itemClass);
                return true;
        }

        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private static string GetDescription(ItemClass itemClass)
    {
        if (itemClass == null || string.IsNullOrEmpty(itemClass.DescriptionKey) || !Localization.Exists(itemClass.DescriptionKey))
            return string.Empty;

        return Localization.Get(itemClass.DescriptionKey);
    }

    private static string GetTierText(ItemClass itemClass)
    {
        if (itemClass == null || !AffixUtils.TryGetAffixTierIndex(itemClass.Name, out int tierIndex))
            return string.Empty;

        return "Tier " + (tierIndex + 1);
    }

    private static string FormatColor(Color32 color)
    {
        return color.r + "," + color.g + "," + color.b + "," + color.a;
    }
}
