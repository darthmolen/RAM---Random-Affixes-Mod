using HarmonyLib;

namespace WeaponAffixesProject.HarmonyPatches;

[HarmonyPatch(typeof(XUiC_CombineGrid), nameof(XUiC_CombineGrid.BtnCombine_OnPressed))]
internal static class CombineGridButtonOverride
{
    private static bool Prefix(XUiC_CombineGrid __instance)
    {
        CombineAffixActions.TryCombine(__instance);
        return false;
    }
}
