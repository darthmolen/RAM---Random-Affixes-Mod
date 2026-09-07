using UnityEngine.Scripting;

[Preserve]
public class XUiC_CombineAffixButton : XUiController
{
    private bool handlingPress;
    private bool boundButton;

    public override void Init()
    {
        base.Init();
        TryBindButton();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (!boundButton)
            TryBindButton();
    }

    private void TryBindButton()
    {
        if (boundButton)
            return;

        OnPress += HandlePress;
        OnMouseUpDown += HandleMouseUpDown;

        XUiController button = GetChildById("button");
        if (button != null)
        {
            button.OnPress += HandlePress;
            button.OnMouseUpDown += HandleMouseUpDown;
        }

        boundButton = true;
    }

    public override void Pressed(int mouseButton)
    {
        DoCombine(mouseButton);
    }

    private void HandlePress(XUiController sender, int mouseButton)
    {
        DoCombine(mouseButton);
    }

    private void HandleMouseUpDown(XUiController sender, bool isDown)
    {
        if (!isDown)
            DoCombine(0);
    }

    private void DoCombine(int mouseButton)
    {
        if (handlingPress)
            return;
        if (mouseButton != 0)
            return;

        handlingPress = true;
        try
        {
            TryCombine();
        }
        finally
        {
            handlingPress = false;
        }
    }

    private void TryCombine()
    {
        XUiC_CombineWindowGroup group = GetParentByType<XUiC_CombineWindowGroup>();
        XUiC_CombineGrid grid = group?.GetChildByType<XUiC_CombineGrid>();
        if (grid == null || grid.result == null || grid.result.IsEmpty())
            return;

        // As of V3.2 the grid owns the whole take-the-result flow — inventory transfer, ammo
        // removal, XP, clearing the slots and the station sound. Route this button through it so
        // it stays identical to the vanilla Combine button; the affix cost is charged by the
        // CombineTakeResultCost patch that wraps it.
        grid.BtnCombine_OnPressed(this, 0);
    }
}
