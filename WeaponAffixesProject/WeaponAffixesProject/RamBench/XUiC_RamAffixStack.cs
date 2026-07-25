using UnityEngine.Scripting;

[Preserve]
public class XUiC_RamAffixStack : XUiC_ItemStack
{
    internal WeaponAffixesProject.RamAffixSource Source { get; private set; }
    public override void Init()
    {
        base.Init();
        AllowDropping = false;
        SimpleClick = false;
        HiddenLock = false;
        OnScroll += HandleScroll;
    }

    public override bool CanSwap(ItemStack itemStack)
    {
        return false;
    }

    public override void HandleClickComplete()
    {
        ToggleSelection();
    }

    private void HandleScroll(XUiController _sender, float delta)
    {
        if (delta != 0f)
            GetParentByType<WeaponAffixesProject.XUiC_RamAffixBrowser>()?.ChangePage(delta < 0f ? 1 : -1);
    }

    public void ToggleSelection()
    {
        if (ItemStack == null || ItemStack.IsEmpty())
            return;

        GetParentByType<WeaponAffixesProject.XUiC_RamAffixGrid>()?.SelectSlot(SlotNumber, ItemStack);
    }

    public override void SwapItem()
    {
    }

    public override void HandleDropOne()
    {
    }

    public override void setItemStack(ItemStack stack)
    {
    }

    public void SetDisplayStack(ItemStack stack, WeaponAffixesProject.RamAffixSource source = null)
    {
        Source = source;
        base.setItemStack(stack);
        IsDirty = true;
        RefreshBindings();
    }


    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "hasitemtypeicon":
            case "isfavorite":
            case "isQuickSwap":
                value = "false";
                return true;
            case "locktypeicon":
            case "stacklockicon":
                value = string.Empty;
                return true;
        }

        return base.GetBindingValueInternal(ref value, bindingName);
    }
}
