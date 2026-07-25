using HarmonyLib;
using System;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemActionList), nameof(XUiC_ItemActionList.SetCraftingActionList))]
    public static class XUiC_ItemActionListSetCraftingActionList
    {
        private static readonly System.Reflection.MethodInfo MI_AddActionListEntry = AccessTools.Method(typeof(XUiC_ItemActionList), "AddActionListEntry");

        private static void Postfix(XUiC_ItemActionList __instance, XUiC_ItemActionList.ItemActionListTypes _actionListType, XUiController itemController)
        {
            try
            {
                if (!(itemController is XUiC_ItemCosmeticStack) && !(itemController is XUiC_ItemPartStack) && !(itemController is XUiC_ItemStack) && !(itemController is XUiC_RamAffixBrowser)) return;

                var xui = __instance.xui;
                if (itemController is XUiC_RamAffixBrowser ramBrowser)
                {
                    if (ramBrowser.IsViewingSourceItem)
                    {
                        MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamTakeSourceItemAction(itemController) });
                        return;
                    }
                    if (ramBrowser.SelectedSource?.ParentStack != null) MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamShowInstalledItemAction(itemController) });
                    if (RamAffixActions.CanExtract(ramBrowser.SelectedSource)) MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamExtractAffixAction(itemController) });
                    if (RamAffixActions.CanUpgrade(ramBrowser.SelectedSource)) MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamUpgradeAffixAction(itemController) });
                    return;
                }
                if (itemController is XUiC_RamAffixStack ramStack)
                {
                    XUiC_RamAffixBrowser browser = ramStack.GetParentByType<XUiC_RamAffixBrowser>();
                    if (browser?.SelectedSource?.ParentStack != null) MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamShowInstalledItemAction(browser) });
                    if (RamAffixActions.CanExtract(browser?.SelectedSource)) MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamExtractAffixAction(browser) });
                    if (RamAffixActions.CanUpgrade(browser?.SelectedSource)) MI_AddActionListEntry?.Invoke(__instance, new object[] { new RamUpgradeAffixAction(browser) });
                    return;
                }
                // Item/Armor one
                if (itemController is XUiC_ItemStack)
                {
                    var weaponController = (XUiC_ItemStack)itemController;
                    var selectedClass = weaponController.itemStack.itemValue.ItemClass;
                    if (selectedClass == null || (!selectedClass.HasAnyTags(AffixUtils.WeaponTag) && !selectedClass.HasAnyTags(AffixUtils.ToolTag) && !selectedClass.HasAnyTags(AffixUtils.ArmorTag)) || selectedClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("noMods")) || selectedClass.HasAnyTags(AffixUtils.UniqueAffixTag)) return;

                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryUnlockAffix(itemController) });
                    if (selectedClass.HasAnyTags(AffixUtils.WeaponTag))
                        MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryAscendWeapon(itemController) });

                    return;
                }

                // Affix specific ones
                var parent = xui?.AssembleItem?.CurrentItem;
                if (parent == null || parent.IsEmpty() || parent.itemValue == null || parent.itemValue.IsEmpty()) return;

                if (itemController is XUiC_ItemCosmeticStack && !parent.itemValue.ItemClass.HasAnyTags(AffixUtils.UniqueAffixTag))
                {
                    var cosmeticController = (XUiC_ItemCosmeticStack)itemController;
                    var selectedClass = cosmeticController.ItemStack?.itemValue?.ItemClass;

                    Log.Out(selectedClass.Name);

                    if (selectedClass == null || !selectedClass.Name.StartsWith("affixMod")) return;

                    if (!AffixUtils.IsGodlikeAffix(selectedClass))
                    {
                        MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryRerollAffix(itemController) });
                        MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryExtractAffix(itemController) });
                    }
                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryUpgradeAffix(itemController) });
                    return;
                }
                else if (itemController is XUiC_ItemPartStack)
                {
                    var partController = (XUiC_ItemPartStack)itemController;
                    var selectedClass2 = partController.ItemStack?.itemValue?.ItemClass;

                    if (selectedClass2 == null || !selectedClass2.Name.StartsWith("affixMod")) return;
                    if (AffixUtils.IsGodlikeAffix(selectedClass2)) return;

                    MI_AddActionListEntry?.Invoke(__instance, new object[] { new ItemActionEntryExtractAffix(itemController) });
                    return;
                }

            }
            catch (Exception e)
            {
                Log.Out($"[REROLL DEBUG] Failed adding reroll/extract action: {e}");
            }
        }
    }
}
