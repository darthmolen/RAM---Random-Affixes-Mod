using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public abstract class XUiC_RamAffixSortButton : XUiController
{
    private bool bound;

    protected abstract bool SortByType { get; }

    public override void Init()
    {
        base.Init();
        Bind();
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        Bind();
    }

    public override void Pressed(int mouseButton)
    {
        Sort(mouseButton);
    }

    private void Bind()
    {
        if (bound)
            return;

        OnPress += OnButtonPressed;
        OnMouseUpDown += OnButtonMouseUpDown;
        bound = true;
    }

    private void OnButtonPressed(XUiController _sender, int mouseButton)
    {
        Sort(mouseButton);
    }

    private void OnButtonMouseUpDown(XUiController _sender, bool isDown)
    {
        if (!isDown)
            Sort(0);
    }

    private void Sort(int mouseButton)
    {
        if (mouseButton != 0)
            return;

        XUiC_RamAffixBrowser browser = GetParentByType<XUiC_RamAffixBrowser>();
        if (SortByType)
            browser?.SortByType();
        else
            browser?.SortByRarity();
    }
}

[Preserve]
public sealed class XUiC_RamAffixRaritySortButton : XUiC_RamAffixSortButton
{
    protected override bool SortByType => false;
}

[Preserve]
public sealed class XUiC_RamAffixTypeSortButton : XUiC_RamAffixSortButton
{
    protected override bool SortByType => true;
}
