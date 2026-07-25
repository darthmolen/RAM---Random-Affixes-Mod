namespace WeaponAffixesProject;

internal sealed class RamUpgradeAffixAction : BaseItemActionEntry
{
    internal RamUpgradeAffixAction(XUiController controller) : base(controller, "lblContextActionUpgradeAffix", "ui_game_symbol_upgrade") { }
    public override void RefreshEnabled() => Enabled = RamAffixActions.CanUpgrade((ItemController as XUiC_RamAffixBrowser)?.SelectedSource);
    public override void OnActivated() => RamAffixActions.Upgrade(ItemController as XUiC_RamAffixBrowser);
}
internal sealed class RamExtractAffixAction : BaseItemActionEntry
{
    internal RamExtractAffixAction(XUiController controller) : base(controller, "lblContextActionExtractAffix", "ui_game_symbol_extract") { }
    public override void RefreshEnabled() => Enabled = RamAffixActions.CanExtract((ItemController as XUiC_RamAffixBrowser)?.SelectedSource);
    public override void OnActivated() => RamAffixActions.Extract(ItemController as XUiC_RamAffixBrowser);
}
internal sealed class RamShowInstalledItemAction : BaseItemActionEntry
{
    internal RamShowInstalledItemAction(XUiController controller) : base(controller, "lblContextActionShowInstalledItem", "ui_game_symbol_search") { }
    public override void RefreshEnabled() => Enabled = (ItemController as XUiC_RamAffixBrowser)?.SelectedSource?.ParentStack != null;
    public override void OnActivated()
    {
        XUiC_RamAffixBrowser browser = ItemController as XUiC_RamAffixBrowser;
        browser?.ShowSourceItem(browser.SelectedSource?.ParentStack);
    }
}
internal sealed class RamTakeSourceItemAction : BaseItemActionEntry
{
    internal RamTakeSourceItemAction(XUiController controller) : base(controller, "lblContextActionTakeSourceItem", "ui_game_symbol_hand") { }
    public override void RefreshEnabled() => Enabled = (ItemController as XUiC_RamAffixBrowser)?.SelectedSource?.ParentStack != null;
    public override void OnActivated() => (ItemController as XUiC_RamAffixBrowser)?.TakeSourceItem();
}
