using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace WeaponAffixesProject;

[Preserve]
public class XUiC_RamAffixBrowser : XUiController
{
    internal XUi Ui => xui;
    internal const int AffixesPerPage = 72;
    private const int ScanRadius = 20;
    private const float RefreshInterval = 5f;
    private const float InitialRefreshDelay = 0.2f;

    private readonly List<ItemStack> affixStacks = new List<ItemStack>();
    private readonly List<RamAffixSource> affixSources = new List<RamAffixSource>();
    private readonly List<ItemStack> filteredAffixStacks = new List<ItemStack>();
    private readonly List<RamAffixSource> filteredAffixSources = new List<RamAffixSource>();
    internal RamAffixSource SelectedSource { get; private set; }
    internal bool IsViewingSourceItem { get; private set; }
    private XUiC_RamAffixStack selectedStackController;
    private XUiC_RamAffixGrid affixGrid;
    private XUiC_TextInput searchInput;
    private XUiC_ItemInfoWindow itemInfoWindow;
    private XUiC_EmptyInfoWindow emptyInfoWindow;
    private float refreshTimer;
    private int containerCount;
    private int itemCount;
    private int currentPage;
    private RamAffixSortMode sortMode;

    public override void Init()
    {
        base.Init();
        affixGrid = GetChildByType<XUiC_RamAffixGrid>();
        TryBindSearchInput();
        FindInspectWindows();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        refreshTimer = InitialRefreshDelay;
        FindInspectWindows();
        ShowEmptyInspect();
        affixStacks.Clear();
        affixSources.Clear();
        SelectedSource = null;
        filteredAffixStacks.Clear();
        containerCount = 0;
        itemCount = 0;
        currentPage = 0;
        sortMode = RamAffixSortMode.Rarity;
        affixGrid?.SetAffixes(filteredAffixStacks, filteredAffixSources, currentPage);
        RefreshBindings();
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        TryBindSearchInput();
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
            case "rampage":
                value = (currentPage + 1).ToString();
                return true;
            case "rampagecount":
                value = PageCount.ToString();
                return true;
            case "ramhaspreviouspage":
                value = (currentPage > 0).ToString().ToLowerInvariant();
                return true;
            case "ramhasnextpage":
                value = (currentPage + 1 < PageCount).ToString().ToLowerInvariant();
                return true;
        }

        return base.GetBindingValueInternal(ref value, bindingName);
    }

    private void RefreshNearbyAffixes()
    {
        if (affixGrid == null)
            affixGrid = GetChildByType<XUiC_RamAffixGrid>();

        affixStacks.Clear();
        affixSources.Clear();
        containerCount = 0;
        itemCount = 0;

        World world = GameManager.Instance?.World;
        if (world == null)
            return;

        Vector3i center = GetScanCenter();
        PersistentPlayerData playerData = GameManager.Instance?.myEntityPlayerLocal?.PersistentPlayerData;
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
                    if (tileEntity == null || !tileEntity.TryGetSelfOrFeature<TEFeatureStorage>(out TEFeatureStorage storage))
                        continue;
                    if (storage.ItemGrid == null || !storage.ItemGrid.PlayerOwned)
                        continue;

                    Vector3i storagePos = storage.ToWorldPos();
                    int dx = storagePos.x - center.x;
                    int dz = storagePos.z - center.z;
                    if (dx * dx + dz * dz > radiusSquared)
                        continue;
                    if (!IsInSameLandClaimArea(world, center, storagePos, playerData))
                        continue;

                    containerCount++;
                    CollectAffixes(storage.ItemGrid);
                }
            }
        }

        ApplyFilter();
        currentPage = Math.Max(0, Math.Min(currentPage, PageCount - 1));
        affixGrid?.SetAffixes(filteredAffixStacks, filteredAffixSources, currentPage);
        RefreshBindings();
    }

    internal void SelectAffix(ItemStack affixStack, RamAffixSource source, XUiC_RamAffixStack stackController = null)
    {
        FindInspectWindows();
        if (affixStack == null || affixStack.IsEmpty())
        {
            ShowEmptyInspect();
            SelectedSource = null;
            return;
        }

        SelectedSource = source;
        IsViewingSourceItem = false;
        selectedStackController = stackController;

        itemInfoWindow?.SetInfo(affixStack.Clone(), (XUiController)selectedStackController ?? this, XUiC_ItemActionList.ItemActionListTypes.None);
        if (itemInfoWindow?.ViewComponent != null)
            itemInfoWindow.ViewComponent.IsVisible = true;
        if (emptyInfoWindow?.ViewComponent != null)
            emptyInfoWindow.ViewComponent.IsVisible = false;
    }

    internal void ChangePage(int direction)
    {
        int newPage = Math.Max(0, Math.Min(currentPage + direction, PageCount - 1));
        if (newPage == currentPage)
            return;

        currentPage = newPage;
        affixGrid ??= GetChildByType<XUiC_RamAffixGrid>();
        affixGrid?.SetAffixes(filteredAffixStacks, filteredAffixSources, currentPage);
        ShowEmptyInspect();
        RefreshBindings();
    }

    internal void RefreshNow()
    {
        RamAffixSource previous = SelectedSource;
        RefreshNearbyAffixes();
        if (previous == null)
        {
            ShowEmptyInspect();
            return;
        }

        for (int i = 0; i < filteredAffixSources.Count; i++)
        {
            RamAffixSource source = filteredAffixSources[i];
            if (!ReferenceEquals(source.ParentStack, previous.ParentStack) || source.IsCosmetic != previous.IsCosmetic || source.Slot != previous.Slot)
                continue;

            currentPage = i / AffixesPerPage;
            affixGrid?.SetAffixes(filteredAffixStacks, filteredAffixSources, currentPage);
            int slot = i % AffixesPerPage;
            XUiC_RamAffixStack controller = affixGrid?.GetRamStack(slot);
            SelectAffix(filteredAffixStacks[i].Clone(), source, controller);
            RefreshBindings();
            return;
        }

        ShowEmptyInspect();
    }

    internal void ShowSourceItem(ItemStack parentStack)
    {
        if (parentStack == null || parentStack.IsEmpty())
            return;

        FindInspectWindows();
        IsViewingSourceItem = true;
        itemInfoWindow?.SetInfo(parentStack.Clone(), this, XUiC_ItemActionList.ItemActionListTypes.None);
        if (itemInfoWindow?.ViewComponent != null)
            itemInfoWindow.ViewComponent.IsVisible = true;
        if (emptyInfoWindow?.ViewComponent != null)
            emptyInfoWindow.ViewComponent.IsVisible = false;
    }

    internal void TakeSourceItem()
    {
        RamAffixSource source = SelectedSource;
        EntityPlayerLocal player = GameManager.Instance?.myEntityPlayerLocal;
        if (!IsViewingSourceItem || source?.ParentStack == null || source.StorageItems == null || player == null)
            return;
        if (!xui.PlayerInventory.AddItemNoPartial(source.ParentStack.Clone(), true))
        {
            GameManager.ShowTooltip(player, Localization.Get("xuiInventoryFullForPickup"), string.Empty, "ui_denied");
            return;
        }
        source.StorageItems[source.ParentSlot] = ItemStack.Empty;
        IsViewingSourceItem = false;
        RefreshNow();
    }


    private int PageCount => Math.Max(1, (filteredAffixStacks.Count + AffixesPerPage - 1) / AffixesPerPage);

    private void HandleSearchChanged(XUiController sender, string text, bool _isUserInput)
    {
        currentPage = 0;
        ApplyFilter();
        affixGrid?.SetAffixes(filteredAffixStacks, filteredAffixSources, currentPage);
        ShowEmptyInspect();
        RefreshBindings();
    }

    private void ApplyFilter()
    {
        filteredAffixStacks.Clear();
        filteredAffixSources.Clear();
        string query = searchInput?.Text?.Trim();

        for (int i = 0; i < affixStacks.Count; i++)
        {
            ItemStack stack = affixStacks[i];
            ItemClass itemClass = stack?.itemValue?.ItemClass;
            if (itemClass == null)
                continue;

            string name = itemClass.GetLocalizedItemName() ?? itemClass.Name ?? string.Empty;
            string description = itemClass.DescriptionKey != null && Localization.Exists(itemClass.DescriptionKey)
                ? Localization.Get(itemClass.DescriptionKey)
                : string.Empty;
            if (string.IsNullOrEmpty(query) ||
                name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (itemClass.Name ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                filteredAffixStacks.Add(stack);
                filteredAffixSources.Add(affixSources[i]);
            }
        }

        SortFilteredAffixes();
    }

    private void TryBindSearchInput()
    {
        if (searchInput != null)
            return;

        searchInput = GetChildById("searchInput") as XUiC_TextInput;
        if (searchInput != null)
            searchInput.OnChangeHandler += HandleSearchChanged;
    }

    internal void SortByRarity() => SetSortMode(RamAffixSortMode.Rarity);

    internal void SortByType() => SetSortMode(RamAffixSortMode.Type);

    private void SetSortMode(RamAffixSortMode mode)
    {
        sortMode = mode;
        currentPage = 0;
        ApplyFilter();
        affixGrid?.SetAffixes(filteredAffixStacks, filteredAffixSources, currentPage);
        ShowEmptyInspect();
        RefreshBindings();
    }

    private void SortFilteredAffixes()
    {
        if (sortMode == RamAffixSortMode.None || filteredAffixStacks.Count < 2)
            return;

        // Keep the affix stack and its storage location together while sorting.
        for (int i = 1; i < filteredAffixStacks.Count; i++)
        {
            ItemStack stack = filteredAffixStacks[i];
            RamAffixSource source = filteredAffixSources[i];
            int j = i - 1;
            while (j >= 0 && CompareAffixes(stack, filteredAffixStacks[j]) < 0)
            {
                filteredAffixStacks[j + 1] = filteredAffixStacks[j];
                filteredAffixSources[j + 1] = filteredAffixSources[j];
                j--;
            }

            filteredAffixStacks[j + 1] = stack;
            filteredAffixSources[j + 1] = source;
        }
    }

    private int CompareAffixes(ItemStack left, ItemStack right)
    {
        string leftName = left?.itemValue?.ItemClass?.Name ?? string.Empty;
        string rightName = right?.itemValue?.ItemClass?.Name ?? string.Empty;

        if (sortMode == RamAffixSortMode.Type)
        {
            int typeComparison = string.Compare(GetAffixType(leftName), GetAffixType(rightName), StringComparison.OrdinalIgnoreCase);
            if (typeComparison != 0)
                return typeComparison;
        }

        int tierComparison = GetAffixTier(rightName).CompareTo(GetAffixTier(leftName));
        if (tierComparison != 0)
            return tierComparison;

        return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetAffixTier(string affixName)
    {
        return AffixUtils.TryGetAffixTierIndex(affixName, out int tier) ? tier : -1;
    }

    private static string GetAffixType(string affixName)
    {
        return AffixUtils.TryGetAffixTierIndex(affixName, out _) && affixName.Length > 1
            ? affixName.Substring(0, affixName.Length - 1)
            : affixName;
    }

    private void FindInspectWindows()
    {
        XUiController backpackGroup = xui?.FindWindowGroupByName("backpack");
        itemInfoWindow ??= backpackGroup?.GetChildById("itemInfoPanel") as XUiC_ItemInfoWindow;
        emptyInfoWindow ??= backpackGroup?.GetChildById("emptyInfoPanel") as XUiC_EmptyInfoWindow;
    }

    private void ShowEmptyInspect()
    {
        FindInspectWindows();
        if (itemInfoWindow?.ViewComponent != null)
            itemInfoWindow.ViewComponent.IsVisible = false;
        if (emptyInfoWindow?.ViewComponent != null)
            emptyInfoWindow.ViewComponent.IsVisible = true;
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

    private static bool IsInSameLandClaimArea(World world, Vector3i benchPosition, Vector3i storagePosition, PersistentPlayerData playerData)
    {
        if (world == null || playerData == null)
            return false;

        EnumLandClaimOwner claimOwner = world.GetLandClaimOwner(benchPosition, playerData);
        if (claimOwner != EnumLandClaimOwner.Self && claimOwner != EnumLandClaimOwner.Ally)
            return false;
        if (world.GetLandClaimOwner(storagePosition, playerData) != claimOwner)
            return false;

        // Land-claim regions do not overlap. Confirming a continuous claimed path
        // between the bench and container prevents a nearby, separate claim from
        // being treated as part of this bench's claim area.
        int steps = Math.Max(Math.Abs(storagePosition.x - benchPosition.x), Math.Abs(storagePosition.z - benchPosition.z));
        for (int step = 1; step < steps; step++)
        {
            Vector3i sample = new Vector3i(
                benchPosition.x + (storagePosition.x - benchPosition.x) * step / steps,
                benchPosition.y,
                benchPosition.z + (storagePosition.z - benchPosition.z) * step / steps);
            if (world.GetLandClaimOwner(sample, playerData) != claimOwner)
                return false;
        }

        return true;
    }

    private void CollectAffixes(ItemStackGrid storedItems)
    {
        for (int i = 0; i < storedItems.Length; i++)
        {
            ItemStack stack = storedItems[i];
            if (stack == null || stack.IsEmpty() || stack.itemValue == null)
                continue;

            itemCount++;
            CollectAffixes(stack.itemValue.cosmeticMods, stack, storedItems, i, true);
            CollectAffixes(stack.itemValue.modifications, stack, storedItems, i, false);
        }
    }

    private void CollectAffixes(ItemValue[] mods, ItemStack parent, ItemStackGrid storageItems, int parentSlot, bool cosmetic)
    {
        if (mods == null)
            return;

        for (int i = 0; i < mods.Length; i++)
        {
            ItemValue mod = mods[i];
            if (mod == null || mod.IsEmpty() || !AffixUtils.IsAffixMod(mod.ItemClass))
                continue;

            affixStacks.Add(new ItemStack(mod.Clone(), 1));
            affixSources.Add(new RamAffixSource { ParentStack = parent, StorageItems = storageItems, ParentSlot = parentSlot, IsCosmetic = cosmetic, Slot = i, Affix = mod });
        }
    }

    private enum RamAffixSortMode
    {
        None,
        Rarity,
        Type
    }
}
