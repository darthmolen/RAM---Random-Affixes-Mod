using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches
{
    /// <summary>
    /// Drops the affix selection when the combine window closes.
    ///
    /// CombineAffixSelectionState is keyed on the live XUiC_CombineWindowGroup, and XUi builds
    /// that controller once and reuses it for every open. Selections were only cleared after a
    /// successful combine, so closing the window part-way through a selection carried those slot
    /// choices into the next pair of items the player put in the grid.
    /// </summary>
    [HarmonyPatch(typeof(XUiC_CombineWindowGroup), nameof(XUiC_CombineWindowGroup.OnClose))]
    internal static class CombineSelectionLifetime
    {
        private static void Postfix(XUiC_CombineWindowGroup __instance)
        {
            CombineAffixSelectionState.Clear(__instance);
        }
    }
}
