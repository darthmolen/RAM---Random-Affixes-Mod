using System.Collections.Generic;

namespace WeaponAffixesProject;

internal static class RamAffixActions
{
    internal static bool CanUpgrade(RamAffixSource s) => s != null && s.IsCosmetic && s.ParentStack?.itemValue?.ItemClass != null && !s.ParentStack.itemValue.ItemClass.HasAnyTags(AffixUtils.UniqueAffixTag) && AffixUtils.TryGetAffixTierIndex(s.Affix.ItemClass.Name, out int tier) && tier < 5;
    internal static bool CanExtract(RamAffixSource s) => s != null && s.ParentStack?.itemValue?.ItemClass != null && !s.ParentStack.itemValue.ItemClass.HasAnyTags(AffixUtils.UniqueAffixTag) && !AffixUtils.IsGodlikeAffix(s.Affix.ItemClass);

    internal static void Upgrade(XUiC_RamAffixBrowser browser)
    {
        RamAffixSource s = browser?.SelectedSource; EntityPlayerLocal p = GameManager.Instance.myEntityPlayerLocal;
        if (!CanUpgrade(s) || p == null || !HasToken(browser, p, "affixUpgradeToken", "ttUpgradeAffixRequiresItem")) return;
        string name = s.Affix.ItemClass.Name;
        if (!AffixSystem.UpgradeAffix(s.ParentStack.itemValue, new List<int> { s.Slot }, ref name)) { Deny(p, "ttFailedToUpgrade"); return; }
        Consume(browser, "affixUpgradeToken"); GameManager.ShowTooltip(p, Localization.Get("ttUpgradeAffixSucces"), string.Empty, "recipe_unlocked"); AffixUtils.ApplyAffixTokenUseEvents("affixUpgradeToken"); browser.RefreshNow();
    }

    internal static void Extract(XUiC_RamAffixBrowser browser)
    {
        RamAffixSource s = browser?.SelectedSource; EntityPlayerLocal p = GameManager.Instance.myEntityPlayerLocal;
        if (!CanExtract(s) || p == null || !HasToken(browser, p, "affixExtractionToken", "ttExtractionAffixRequiresItem")) return;
        if (!browser.Ui.PlayerInventory.AddItemNoPartial(new ItemStack(s.Affix.Clone(), 1), true)) { Deny(p, "xuiInventoryFullForPickup"); return; }
        if (s.IsCosmetic) { var old=s.ParentStack.itemValue.cosmeticMods; var list=new List<ItemValue>(); for(int i=0;i<old.Length;i++) if(i!=s.Slot) list.Add(old[i]); s.ParentStack.itemValue.SetCosmeticMods(list.ToArray()); }
        else s.ParentStack.itemValue.SetModification(s.Slot, null);
        Consume(browser, "affixExtractionToken"); GameManager.ShowTooltip(p, Localization.Get("ttExtractionAffixSucces"), string.Empty, "recipe_unlocked"); AffixUtils.ApplyAffixTokenUseEvents("affixExtractionToken"); browser.RefreshNow();
    }

    private static bool HasToken(XUiC_RamAffixBrowser b, EntityPlayerLocal p, string token, string missing)
    { var c=ItemClass.GetItemClass(token); if(c==null) return false; if(b.Ui.PlayerInventory.GetItemCount(new ItemValue(c.Id))>0) return true; b.Ui.CollectedItemList?.AddItemStack(new ItemStack(new ItemValue(c.Id),0)); Deny(p,string.Format(Localization.Get(missing),c.localizedName)); return false; }
    private static void Consume(XUiC_RamAffixBrowser b,string token) { var c=ItemClass.GetItemClass(token); var v=new ItemValue(c.Id); b.Ui.PlayerInventory.RemoveItems(new List<ItemStack>{new ItemStack(v,1)}); b.Ui.CollectedItemList?.RemoveItemStack(new ItemStack(v,1)); }
    private static void Deny(EntityPlayerLocal p,string key) => GameManager.ShowTooltip(p,Localization.Get(key),string.Empty,"ui_denied");
}
