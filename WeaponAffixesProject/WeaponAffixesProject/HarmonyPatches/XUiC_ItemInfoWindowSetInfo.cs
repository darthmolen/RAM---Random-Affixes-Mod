using HarmonyLib;

namespace WeaponAffixesProject
{
    // V3.3's SetInfo fills the part list with the item's cosmetics and then its
    // modifications, bounded by the slots the grid actually has, so affixes already show
    // up in the mod preview. This used to re-implement that tail of SetInfo through
    // XUiC_PartList.SetSlots, which V3.3 removed. What is left is RAM's own business:
    // swapping the bench's empty-info placeholder for the real panel.

    [HarmonyPatch(typeof(XUiC_ItemInfoWindow), nameof(XUiC_ItemInfoWindow.SetInfo))]
    public static class XUiC_ItemInfoWindowSetInfo
    {
        private static void Postfix(XUiC_ItemInfoWindow __instance)
        {
            XUiC_RamBenchWindowGroup ramBench = __instance.GetParentByType<XUiC_RamBenchWindowGroup>();
            if (ramBench == null)
                return;

            XUiController emptyInfo = ramBench.GetChildById("emptyInfoPanel");
            if (emptyInfo?.ViewComponent != null)
                emptyInfo.ViewComponent.IsVisible = false;
            if (__instance.ViewComponent != null)
                __instance.ViewComponent.IsVisible = true;
        }
    }
}
