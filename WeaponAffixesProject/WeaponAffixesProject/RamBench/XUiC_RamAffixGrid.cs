using System.Collections.Generic;
using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public class XUiC_RamAffixGrid : XUiC_ItemStackGrid
{
    private int selectedSlot = -1;

    public override XUiC_ItemStack.StackLocationTypes StackLocation =>
        XUiC_ItemStack.StackLocationTypes.Creative;

    public void SetAffixes(List<ItemStack> affixes)
    {
        if (itemControllers == null)
            return;

        int affixCount = affixes?.Count ?? 0;
        for (int i = 0; i < itemControllers.Length; i++)
        {
            XUiC_ItemStack controller = itemControllers[i];
            bool hasAffix = i < affixCount;
            ItemStack displayStack = hasAffix ? affixes[i].Clone() : ItemStack.Empty.Clone();
            if (controller is XUiC_RamAffixStack affixController)
                affixController.SetDisplayStack(displayStack);
            else
                controller.ItemStack = displayStack;
            controller.SlotNumber = i;
            controller.StackLocation = StackLocation;
            controller.ViewComponent.IsVisible = hasAffix;
            controller.IsSelected = hasAffix && i == selectedSlot;
        }

        RefreshBindingsSelfAndChildren();
    }

    internal void SelectSlot(int slotNumber, ItemStack stack)
    {
        if (stack == null || stack.IsEmpty())
            return;

        selectedSlot = selectedSlot == slotNumber ? -1 : slotNumber;
        XUiC_RamAffixBrowser browser = GetParentByType<XUiC_RamAffixBrowser>();
        browser?.SelectAffix(selectedSlot == -1 ? ItemStack.Empty : stack.Clone());

        if (itemControllers != null)
        {
            for (int i = 0; i < itemControllers.Length; i++)
                itemControllers[i].IsSelected = i == selectedSlot;
        }

        RefreshBindingsSelfAndChildren();
    }

    public override void HandleSlotChangedEvent(int slotNumber, ItemStack stack)
    {
    }

    public override void UpdateBackend(ItemStack[] stackList)
    {
    }
}
