using HarmonyLib;

namespace WeaponAffixesProject
{
    // Makes it such that all affixes show up in the mod preview on an item

    [HarmonyPatch(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.SetInfo))]
    public static class XUiC_ItemInfoWindowSetInfo
    {
        private static void Postfix(XUiC_ItemInfoWindow __instance, ItemStack stack)
        {
            XUiC_RamBenchWindowGroup ramBench = __instance.GetParentByType<XUiC_RamBenchWindowGroup>();
            if (ramBench != null)
            {
                XUiController emptyInfo = ramBench.GetChildById("emptyInfoPanel");
                if (emptyInfo?.ViewComponent != null)
                    emptyInfo.ViewComponent.IsVisible = false;
                if (__instance.ViewComponent != null)
                    __instance.ViewComponent.IsVisible = true;
            }

            bool flag = stack.itemValue.type == __instance.itemStack.itemValue.type && stack.count == __instance.itemStack.count;
            __instance.itemStack = stack.Clone();
            bool flag2 = __instance.itemStack != null && !__instance.itemStack.IsEmpty();
            if (__instance.itemPreview == null)
            {
                return;
            }
            if (!flag || !stack.itemValue.Equals(__instance.itemStack.itemValue))
            {
                __instance.compareStack = ItemStack.Empty.Clone();
            }
            if (flag2 && __instance.itemStack.itemValue.modifications != null)
            {
                __instance.partList.SetMainItem(__instance.itemStack);
                if (__instance.itemStack.itemValue.cosmeticMods != null && __instance.itemStack.itemValue.cosmeticMods.Length != 0 && __instance.itemStack.itemValue.cosmeticMods[0] != null && !__instance.itemStack.itemValue.cosmeticMods[0].IsEmpty())
                {
                    __instance.partList.SetSlots(__instance.itemStack.itemValue.cosmeticMods, 0);
                    __instance.partList.SetSlots(__instance.itemStack.itemValue.modifications, __instance.itemStack.itemValue.cosmeticMods.Length);
                }
                else
                {
                    __instance.partList.SetSlots(__instance.itemStack.itemValue.modifications, 0);
                }
                __instance.partList.ViewComponent.IsVisible = true;
            }
        }
    }
}
