using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public class XUiC_RamAffixBrowser : XUiController
{
    private const int ScanRadius = 20;
    private const float RefreshInterval = 5f;
    private const float InitialRefreshDelay = 0.2f;

    private readonly List<ItemStack> affixStacks = new List<ItemStack>();
    private XUiC_RamAffixGrid affixGrid;
    private XUiC_RamAffixDetails detailsWindow;
    private float refreshTimer;
    private int containerCount;
    private int itemCount;

    public override void Init()
    {
        base.Init();
        affixGrid = GetChildByType<XUiC_RamAffixGrid>();
        detailsWindow = GetParentByType<XUiC_RamBenchWindowGroup>()?.GetChildByType<XUiC_RamAffixDetails>();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        refreshTimer = InitialRefreshDelay;
        detailsWindow = GetParentByType<XUiC_RamBenchWindowGroup>()?.GetChildByType<XUiC_RamAffixDetails>();
        detailsWindow?.SetSelectedAffix(ItemStack.Empty);
        affixStacks.Clear();
        containerCount = 0;
        itemCount = 0;
        affixGrid?.SetAffixes(affixStacks);
        RefreshBindings();
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        refreshTimer -= dt;
        if (refreshTimer <= 0f)
        {
            RefreshNearbyAffixes();
            refreshTimer = RefreshInterval;
        }
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "ramcontainers":
                value = containerCount.ToString();
                return true;
            case "ramitems":
                value = itemCount.ToString();
                return true;
            case "ramaffixes":
                value = affixStacks.Count.ToString();
                return true;
            case "ramhasaffixes":
                value = (affixStacks.Count > 0).ToString().ToLowerInvariant();
                return true;
            case "ramnoaffixes":
                value = (affixStacks.Count == 0).ToString().ToLowerInvariant();
                return true;
        }

        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void RefreshNearbyAffixes()
    {
        if (affixGrid == null)
            affixGrid = GetChildByType<XUiC_RamAffixGrid>();

        affixStacks.Clear();
        containerCount = 0;
        itemCount = 0;

        World world = GameManager.Instance?.World;
        if (world == null)
            return;

        Vector3i center = GetScanCenter();
        int minChunkX = World.toChunkXZ(center.x - ScanRadius);
        int maxChunkX = World.toChunkXZ(center.x + ScanRadius);
        int minChunkZ = World.toChunkXZ(center.z - ScanRadius);
        int maxChunkZ = World.toChunkXZ(center.z + ScanRadius);
        int radiusSquared = ScanRadius * ScanRadius;

        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
        {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; chunkZ++)
            {
                Chunk chunk = world.GetChunkSync(chunkX, chunkZ) as Chunk;
                if (chunk == null)
                    continue;

                List<TileEntity> tileEntities = chunk.GetTileEntities().list;
                for (int i = 0; i < tileEntities.Count; i++)
                {
                    TileEntity tileEntity = tileEntities[i];
                    if (tileEntity == null || !tileEntity.TryGetSelfOrFeature<ITileEntityLootable>(out ITileEntityLootable storage))
                        continue;
                    if (!storage.bPlayerStorage || storage.items == null)
                        continue;

                    Vector3i storagePos = storage.ToWorldPos();
                    int dx = storagePos.x - center.x;
                    int dz = storagePos.z - center.z;
                    if (dx * dx + dz * dz > radiusSquared)
                        continue;

                    containerCount++;
                    CollectAffixes(storage.items);
                }
            }
        }

        affixGrid?.SetAffixes(affixStacks);
        if (detailsWindow != null && (detailsWindow.SelectedAffix.IsEmpty() || !ContainsAffix(detailsWindow.SelectedAffix)))
            detailsWindow.SetSelectedAffix(ItemStack.Empty);
        RefreshBindings();
    }

    internal void SelectAffix(ItemStack affixStack)
    {
        detailsWindow ??= GetParentByType<XUiC_RamBenchWindowGroup>()?.GetChildByType<XUiC_RamAffixDetails>();
        detailsWindow?.SetSelectedAffix(affixStack);
    }

    private bool ContainsAffix(ItemStack selected)
    {
        if (selected == null || selected.IsEmpty())
            return false;

        for (int i = 0; i < affixStacks.Count; i++)
        {
            ItemStack affixStack = affixStacks[i];
            if (affixStack != null && !affixStack.IsEmpty() && affixStack.itemValue.Equals(selected.itemValue))
                return true;
        }

        return false;
    }

    private Vector3i GetScanCenter()
    {
        XUiC_WorkstationWindowGroup workstation = GetParentByType<XUiC_WorkstationWindowGroup>();
        TileEntityWorkstation tileEntity = workstation?.WorkstationData?.TileEntity;
        if (tileEntity != null)
            return tileEntity.ToWorldPos();

        return xui?.playerUI?.entityPlayer?.GetBlockPosition() ?? Vector3i.zero;
    }

    private void CollectAffixes(ItemStack[] storedItems)
    {
        for (int i = 0; i < storedItems.Length; i++)
        {
            ItemStack stack = storedItems[i];
            if (stack == null || stack.IsEmpty() || stack.itemValue == null)
                continue;

            itemCount++;
            CollectAffixes(stack.itemValue.CosmeticMods);
            CollectAffixes(stack.itemValue.Modifications);
        }
    }

    private void CollectAffixes(ItemValue[] mods)
    {
        if (mods == null)
            return;

        for (int i = 0; i < mods.Length; i++)
        {
            ItemValue mod = mods[i];
            if (mod == null || mod.IsEmpty() || !AffixUtils.IsAffixMod(mod.ItemClass))
                continue;

            affixStacks.Add(new ItemStack(mod.Clone(), 1));
        }
    }
}
