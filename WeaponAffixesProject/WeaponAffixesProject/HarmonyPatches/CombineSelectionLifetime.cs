using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches
{
    /// <summary>
    /// Drops the affix selection when the combine window closes.
    ///
    /// CombineAffixSelectionState is keyed on the live XUiC_CombineWindowGroup, and XUi builds
    /// that controller once and reuses it for every open. CombineAffixActions.TryCombine clears
    /// the state after a successful combine, but closing the window part-way through a selection
    /// left it set, so those slot choices carried into the next pair of items the player put in
    /// the grid.
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
