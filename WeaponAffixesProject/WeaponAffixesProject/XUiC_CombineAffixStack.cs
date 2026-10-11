using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

[Preserve]
public class XUiC_CombineAffixStack : XUiC_ItemStack
{
    private static readonly List<XUiC_CombineAffixStack> ActiveStacks = new List<XUiC_CombineAffixStack>();

    private bool useItemB;
    private int affixSlot;
    private int lastType = int.MinValue;
    private bool refreshingDisplayStack;
    private bool registered;

    public override void Init()
    {
        base.Init();
        AllowDropping = false;
        SimpleClick = true;
        IsDirty = true;
        ActiveStacks.Add(this);
        registered = true;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        RefreshAffixStack();
    }

    public override bool CanSwap(ItemStack _itemStack)
    {
        return false;
    }

    public override void HandleClickComplete()
    {
        ToggleSelection();
    }

    public void ToggleSelection()
    {
        if (!HasAffix())
            return;
        if (useItemB && IsGodlikeAffix())
            return;

        XUiC_CombineWindowGroup group = GetCombineWindowGroup();
        if (group == null)
            return;

        WeaponAffixesProject.CombineAffixSelectionState.Toggle(group, affixSlot, useItemB);
        RefreshSelectionBindings(group, affixSlot);

        XUiC_CombineGrid grid = GetCombineGrid(group);
        if (grid != null)
            grid.Merge_SlotChangedEvent(0, grid.merge1.ItemStack);
    }

    public override void SwapItem()
    {
    }

    public override void HandleDropOne()
    {
    }

    public override void setItemStack(ItemStack stack)
    {
        if (refreshingDisplayStack)
            base.setItemStack(stack);
    }

    // V3.3 answers the {selectionbordercolor} binding through its own
    // bindingSelectionBorderColor() method, which XUi resolves ahead of
    // GetBindingValueInternal, so the override that used to return the highlight here was
    // never consulted. XUiC_CombineAffixStackReadOnly paints it onto the SelectionBorderColor
    // property instead, and reads this to decide when.
    internal bool IsAffixSelected =>
        WeaponAffixesProject.CombineAffixSelectionState.IsSelected(GetCombineWindowGroup(), affixSlot, useItemB);

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

    public override bool ParseAttribute(string name, string value)
    {
        switch (name)
        {
            case "affix_source":
                useItemB = value.Equals("B", StringComparison.OrdinalIgnoreCase);
                return true;
            case "affix_slot":
                int.TryParse(value, out affixSlot);
                return true;
            case "prefix_id":
                if (TryParseSlotKey(value))
                    return true;
                break;
        }

        return base.ParseAttribute(name, value);
    }

    private bool TryParseSlotKey(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 2)
            return false;

        char source = char.ToUpperInvariant(value[0]);
        if (source != 'A' && source != 'B')
            return false;
        if (!int.TryParse(value.Substring(1), out int slot))
            return false;

        useItemB = source == 'B';
        affixSlot = slot;
        return true;
    }

    private void RefreshAffixStack()
    {
        int maxAffixes = GetConfiguredMaxAffixes();
        if (ViewComponent != null)
            ViewComponent.IsVisible = affixSlot < maxAffixes;

        ItemValue newAffix = GetAffix();
        XUiC_CombineWindowGroup group = GetCombineWindowGroup();
        if (newAffix == null || newAffix.IsEmpty() || useItemB && IsGodlikeAffix())
        {
            WeaponAffixesProject.CombineAffixSelectionState.ClearIfSelected(group, affixSlot, useItemB);
        }

        int type = newAffix == null || newAffix.IsEmpty() ? 0 : newAffix.type;
        if (!IsDirty && type == lastType)
            return;

        lastType = type;
        refreshingDisplayStack = true;
        try
        {
            ItemStack = type == 0 ? ItemStack.Empty.Clone() : new ItemStack(newAffix.Clone(), 1);
        }
        finally
        {
            refreshingDisplayStack = false;
        }
        IsDirty = false;
        RefreshBindings();
    }

    private static void RefreshSelectionBindings(XUiC_CombineWindowGroup group, int slot)
    {
        for (int i = ActiveStacks.Count - 1; i >= 0; i--)
        {
            XUiC_CombineAffixStack stack = ActiveStacks[i];
            if (stack == null || !stack.registered || stack.ViewComponent == null)
            {
                ActiveStacks.RemoveAt(i);
                continue;
            }

            if (stack.affixSlot != slot || stack.GetCombineWindowGroup() != group)
                continue;

            stack.RefreshBindings();
            stack.ViewComponent.IsVisible = slot < GetConfiguredMaxAffixes();
        }
    }

    private ItemValue GetAffix()
    {
        XUiC_CombineWindowGroup group = GetCombineWindowGroup();
        XUiC_CombineGrid grid = GetCombineGrid(group);
        if (grid == null)
            return null;

        ItemStack stack = useItemB ? grid.merge2.ItemStack : grid.merge1.ItemStack;
        if (stack == null || stack.IsEmpty() || stack.itemValue == null || stack.itemValue.cosmeticMods == null)
            return null;
        if (affixSlot < 0 || affixSlot >= stack.itemValue.cosmeticMods.Length)
            return null;

        ItemValue mod = stack.itemValue.cosmeticMods[affixSlot];
        if (mod == null || mod.IsEmpty() || !WeaponAffixesProject.AffixUtils.IsAffixMod(mod.ItemClass))
            return null;

        return mod;
    }

    private bool HasAffix()
    {
        ItemValue affix = GetAffix();
        return affix != null && !affix.IsEmpty();
    }

    private bool IsGodlikeAffix()
    {
        return WeaponAffixesProject.CombineAffixSelectionState.IsGodlikeAffix(GetAffix());
    }

    private XUiC_CombineWindowGroup GetCombineWindowGroup()
    {
        return GetParentByType<XUiC_CombineWindowGroup>();
    }

    private static XUiC_CombineGrid GetCombineGrid(XUiC_CombineWindowGroup group)
    {
        return group == null ? null : group.GetChildByType<XUiC_CombineGrid>();
    }

    private static int GetConfiguredMaxAffixes()
    {
        return WeaponAffixesProject.AffixUtils.GetConfiguredMaxAffixes();
    }
}
