using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public class XUiC_RamBenchWindowGroup : XUiC_WorkstationWindowGroup
{
    public override void OnOpen()
    {
        base.OnOpen();

        HideChildWindow("windowCraftingList");
        HideChildWindow("craftingInfoPanel");
        HideChildWindow("windowCraftingQueue");
        xui.RecenterWindowGroup(windowGroup);
    }

    private void HideChildWindow(string id)
    {
        XUiController child = GetChildById(id);
        if (child?.ViewComponent != null)
            child.ViewComponent.IsVisible = false;
    }
}
