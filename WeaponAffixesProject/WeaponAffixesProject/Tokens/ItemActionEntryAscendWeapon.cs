using System;
using System.Collections.Generic;

namespace WeaponAffixesProject
{
    internal class ItemActionEntryAscendWeapon : BaseItemActionEntry
    {
        private const string AscensionTokenName = "affixAscensionToken";
        private const string AscendWeaponChallengeEvent = "affixModGiantslayer7";
        private const string AscendWeaponThreeTimesChallengeEvent = "affixModBuffDuration7";
        private const int KillsPerAscension = 5000;
        private const int BaseMaxAscensions = 3;
        private const int LegendaryMaxAscensions = 4;

        public ItemActionEntryAscendWeapon(XUiController controller)
            : base(controller, "lblContextActionAscendWeapon", "ui_game_symbol_ascend", BaseItemActionEntry.GamepadShortCut.None, "crafting/craft_click_craft", "ui/ui_denied")
        {
        }

        public override void RefreshEnabled()
        {
            base.Enabled = true;
        }

        public override void OnActivated()
        {
            XUiC_ItemStack itemController = ItemController as XUiC_ItemStack;
            ItemStack weaponStack = itemController?.itemStack;
            ItemValue weapon = weaponStack?.itemValue;
            EntityPlayerLocal player = GameManager.Instance?.myEntityPlayerLocal;
            XUiM_PlayerInventory inventory = ItemController?.xui?.PlayerInventory;
            if (weapon?.ItemClass == null || player == null || inventory == null)
                return;
            if (!weapon.ItemClass.HasAnyTags(AffixUtils.WeaponTag))
                return;

            weapon.TryGetMetadata(WeaponUpgrades.AscensionsMetadataKey, out float ascensions);
            int maxAscensions = AffixUtils.ChallengeGroupIsCompleted(player, "ram legendary")
                ? LegendaryMaxAscensions
                : BaseMaxAscensions;
            if (ascensions >= maxAscensions)
            {
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttAscendWeaponMaxAscensions"), maxAscensions), string.Empty, "ui_denied");
                return;
            }

            ItemClass tokenClass = ItemClass.GetItemClass(AscensionTokenName, false);
            if (tokenClass == null)
            {
                Log.Out($"[Affix] Ascension token '{AscensionTokenName}' was not found.");
                return;
            }

            ItemValue token = new ItemValue(tokenClass.Id, false);
            if (inventory.GetItemCount(token) < 1)
            {
                ItemController.xui.CollectedItemList?.AddItemStack(new ItemStack(token, 0), false);
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttAscendWeaponRequiresToken"), tokenClass.localizedName), string.Empty, "ui_denied");
                return;
            }

            weapon.TryGetMetadata(WeaponUpgrades.KillsMetadataKey, out float kills);
            int requiredKills = KillsPerAscension * ((int)ascensions + 1);
            if (kills < requiredKills)
            {
                GameManager.ShowTooltip(player, string.Format(Localization.Get("ttAscendWeaponRequiresKills"), requiredKills, (int)kills), string.Empty, "ui_denied");
                return;
            }

            List<int> mythicSlots = GetMythicAffixSlots(weapon);
            if (mythicSlots.Count == 0)
            {
                GameManager.ShowTooltip(player, Localization.Get("ttAscendWeaponRequiresMythic"), string.Empty, "ui_denied");
                return;
            }

            bool isFourthAscension = ascensions >= BaseMaxAscensions;
            int requestedUpgrades = isFourthAscension
                ? mythicSlots.Count
                : AffixUtils.GetConfiguredMaxAffixes() <= 6 ? 1 : 2;
            int ascendedAffixes = AscendMythicAffixes(weapon, mythicSlots, requestedUpgrades);
            if (ascendedAffixes == 0)
            {
                GameManager.ShowTooltip(player, Localization.Get("ttAscendWeaponFailed"), string.Empty, "ui_denied");
                return;
            }

            WeaponUpgrades.ResetUpgradeCycleForAscension(weapon, player);
            float newAscensions = ascensions + 1;
            weapon.SetMetadata(WeaponUpgrades.AscensionsMetadataKey, newAscensions);

            List<ItemStack> cost = new List<ItemStack> { new ItemStack(token, 1) };
            inventory.RemoveItems(cost, 1, null);
            ItemController.xui.CollectedItemList?.RemoveItemStack(new ItemStack(token, 1));
            AffixUtils.ApplyAffixTokenUseEvents(AscensionTokenName);
            AffixUtils.ApplyQuestEventManagerUseItem(AscendWeaponChallengeEvent);
            if (newAscensions == BaseMaxAscensions)
                AffixUtils.ApplyQuestEventManagerUseItem(AscendWeaponThreeTimesChallengeEvent);

            itemController.itemStack = weaponStack;
            RefreshAssembleWindow(itemController, weaponStack);
            GameManager.ShowTooltip(player, string.Format(Localization.Get("ttAscendWeaponSuccess"), ascendedAffixes, (int)ascensions + 1), string.Empty, "recipe_unlocked");
        }

        private static List<int> GetMythicAffixSlots(ItemValue weapon)
        {
            List<int> slots = new List<int>();
            if (weapon.cosmeticMods == null)
                return slots;

            for (int i = 0; i < weapon.cosmeticMods.Length; i++)
            {
                ItemValue affix = weapon.cosmeticMods[i];
                if (affix?.ItemClass == null || !AffixUtils.IsAffixMod(affix.ItemClass))
                    continue;
                if (AffixUtils.TryGetAffixTierIndex(affix.ItemClass.Name, out int tierIndex) &&
                    tierIndex + 1 == AffixUtils.MaxNormallyObtainableAffixTier)
                    slots.Add(i);
            }

            return slots;
        }

        private static int AscendMythicAffixes(ItemValue weapon, List<int> mythicSlots, int requestedUpgrades)
        {
            int upgradeCount = Math.Min(requestedUpgrades, mythicSlots.Count);
            int ascended = 0;

            for (int i = 0; i < upgradeCount; i++)
            {
                int selectedListIndex = AffixUtils.rng.Next(mythicSlots.Count);
                int affixSlot = mythicSlots[selectedListIndex];
                mythicSlots.RemoveAt(selectedListIndex);

                ItemClassModifier oldAffix = weapon.cosmeticMods[affixSlot].ItemClass as ItemClassModifier;
                if (oldAffix == null)
                    continue;

                string godlikeName = oldAffix.Name.Substring(0, oldAffix.Name.Length - 1) + AffixUtils.MaxRecognizedAffixTier;
                if (!(ItemClass.GetItemClass(godlikeName, false) is ItemClassModifier godlikeAffix))
                {
                    Log.Out($"[Affix] Godlike affix '{godlikeName}' was not found.");
                    continue;
                }

                if (oldAffix.Name.Contains("KillStreak"))
                    GameManager.Instance?.myEntityPlayerLocal?.Buffs?.RemoveBuff("buff" + oldAffix.Name);

                weapon.SetCosmeticMod(affixSlot, new ItemValue(godlikeAffix.Id));
                ascended++;
            }

            return ascended;
        }

        private static void RefreshAssembleWindow(XUiC_ItemStack itemController, ItemStack itemStack)
        {
            XUi xui = itemController.xui;
            if (xui.AssembleItem != null)
            {
                xui.AssembleItem.currentItem = itemStack;
                xui.AssembleItem.RefreshAssembleItem();
            }

            var assembleGroup = xui.FindWindowGroupByName("assemble");
            XUiC_ItemCosmeticStackGrid cosmeticGrid = assembleGroup?.GetChildByType<XUiC_ItemCosmeticStackGrid>();
            XUiC_AssembleWindow assembleWindow = assembleGroup?.GetChildByType<XUiC_AssembleWindow>();
            if (cosmeticGrid == null || assembleWindow == null)
                return;

            cosmeticGrid.AssembleWindow = assembleWindow;
            cosmeticGrid.CurrentItem = itemStack;
            cosmeticGrid.SetParts();
            assembleWindow.ItemStack = itemStack;
            assembleWindow.OnChanged();
        }
    }
}
