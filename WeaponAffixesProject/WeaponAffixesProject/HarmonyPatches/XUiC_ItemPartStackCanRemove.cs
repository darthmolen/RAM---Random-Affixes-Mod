using HarmonyLib;
using System;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(XUiC_ItemPartStack), nameof(XUiC_ItemPartStack.CanRemove))]
    public static class XUiC_ItemPartStackCanRemove
    {
        private static bool Prefix(XUiC_ItemPartStack __instance, ref bool __result)
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
