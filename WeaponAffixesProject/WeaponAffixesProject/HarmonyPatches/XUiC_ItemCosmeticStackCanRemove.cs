using HarmonyLib;
using System;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemCosmeticStack), nameof(XUiC_ItemCosmeticStack.CanRemove))]
    public static class XUiC_ItemCosmeticStackCanRemove
    {
        private static bool Prefix(XUiC_ItemCosmeticStack __instance, ref bool __result)
        {
            try
            {
                if (AffixUtils.IsCalledFromExtractAffix()) return true;
                if (AffixUtils.IsAffixMod(__instance.ItemClass)) return __result = false;
            }
            catch (Exception ex)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out($"[WeaponBuffMod] Error in CanRemove_Prefix: '{ex}'");
            }
            return true;
        }
    }
}
