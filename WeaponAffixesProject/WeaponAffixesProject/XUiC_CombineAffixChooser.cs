using UnityEngine.Scripting;

[Preserve]
public class XUiC_CombineAffixChooser : XUiController
{
    private const int Width = 153;
    private const int HeaderHeight = 46;
    private const int CellHeight = 75;
    private int lastMaxAffixes = -1;

    public override void Init()
    {
        base.Init();
        ApplyConfiguredSize();
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        ApplyConfiguredSize();
    }

    private void ApplyConfiguredSize()
    {
        // The panel is just the two affix columns now; V3.2 gave the combine window its own
        // Combine button, so RAM no longer carries one of its own down here.
        int maxAffixes = GetConfiguredMaxAffixes();
        int contentHeight = maxAffixes * CellHeight;
        int windowHeight = HeaderHeight + contentHeight;

        Resize(ViewComponent, Width, windowHeight);

        XUiController content = GetChildById("content");
        if (content != null)
        {
            Resize(content.ViewComponent, Width, contentHeight);
            ResizeChild(content, "backgroundMain", Width, contentHeight);
            ResizeChild(content, "background", Width, contentHeight);
        }

        lastMaxAffixes = maxAffixes;
    }

    private static void ResizeChild(XUiController parent, string childId, int width, int height)
    {
        XUiController child = parent.GetChildById(childId);
        if (child != null)
            Resize(child.ViewComponent, width, height);
    }

    private static void Resize(XUiView view, int width, int height)
    {
        if (view == null)
            return;

        view.Width = width;
        view.Height = height;
    }

    private static int GetConfiguredMaxAffixes()
    {
        return WeaponAffixesProject.AffixUtils.GetConfiguredMaxAffixes();
    }
}
