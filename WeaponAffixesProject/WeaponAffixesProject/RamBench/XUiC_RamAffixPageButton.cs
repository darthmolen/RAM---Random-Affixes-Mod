using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public abstract class XUiC_RamAffixPageButton : XUiController
{
    private bool bound;

    protected abstract int Direction { get; }

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
        ChangePage(mouseButton);
    }

    private void Bind()
    {
        if (bound)
            return;

        OnPress += OnButtonPressed;
        OnMouseUpDown += OnPageMouseUpDown;
        XUiController button = GetChildById("button");
        if (button != null)
        {
            button.OnPress += OnButtonPressed;
            button.OnMouseUpDown += OnPageMouseUpDown;
        }
        bound = true;
    }

    private void OnButtonPressed(XUiController sender, int mouseButton)
    {
        ChangePage(mouseButton);
    }

    private void OnPageMouseUpDown(XUiController sender, bool isDown)
    {
        if (!isDown)
            ChangePage(0);
    }

    private void ChangePage(int mouseButton)
    {
        if (mouseButton == 0)
            GetParentByType<XUiC_RamAffixBrowser>()?.ChangePage(Direction);
    }
}

[Preserve]
public sealed class XUiC_RamAffixPreviousPageButton : XUiC_RamAffixPageButton
{
    protected override int Direction => -1;
}

[Preserve]
public sealed class XUiC_RamAffixNextPageButton : XUiC_RamAffixPageButton
{
    protected override int Direction => 1;
}
