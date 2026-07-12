using UnityEngine.Scripting;

[Preserve]
public class XUiC_RamAffixStack : XUiC_ItemStack
{
    public override void Init()
    {
        base.Init();
        AllowDropping = false;
        SimpleClick = false;
        HiddenLock = false;
    }

    public override bool CanSwap(ItemStack itemStack)
    {
        return false;
    }

    public override void HandleClickComplete()
    {
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

    public void SetDisplayStack(ItemStack stack)
    {
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
