using System.Collections.Generic;
using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public class XUiC_RamAffixGrid : XUiC_ItemStackGrid
{
    private int selectedSlot = -1;

    public override XUiC_ItemStack.StackLocationTypes StackLocation =>
        XUiC_ItemStack.StackLocationTypes.Creative;

    internal XUiC_RamAffixStack GetRamStack(int slot) =>
        itemControllers != null && slot >= 0 && slot < itemControllers.Length ? itemControllers[slot] as XUiC_RamAffixStack : null;

    public void SetAffixes(List<ItemStack> affixes, List<RamAffixSource> sources, int page)
    {
        if (itemControllers == null)
            return;

        int affixCount = affixes?.Count ?? 0;
        int firstAffix = page * XUiC_RamAffixBrowser.AffixesPerPage;
        for (int i = 0; i < itemControllers.Length; i++)
        {
            XUiC_ItemStack controller = itemControllers[i];
            int affixIndex = firstAffix + i;
            bool hasAffix = affixIndex < affixCount;
            ItemStack displayStack = hasAffix ? affixes[affixIndex].Clone() : ItemStack.Empty.Clone();
            if (controller is XUiC_RamAffixStack affixController)
                affixController.SetDisplayStack(displayStack, hasAffix ? sources[affixIndex] : null);
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
        RamAffixSource source = selectedSlot >= 0 && itemControllers[selectedSlot] is XUiC_RamAffixStack ram ? ram.Source : null;
        browser?.SelectAffix(selectedSlot == -1 ? ItemStack.Empty : stack.Clone(), source, selectedSlot >= 0 ? itemControllers[selectedSlot] as XUiC_RamAffixStack : null);

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
