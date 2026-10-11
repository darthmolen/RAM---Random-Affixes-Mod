using System;
using System.Collections.Generic;
using System.Linq;

namespace WeaponAffixesProject
{
    internal static class AffixSystem
    {
        private const string AffixesRolledMetadataKey = "ramAffixesRolled";

        public static void ApplyAffixToLootCheck(List<ItemStack> spawnedItems, EntityPlayer player)
        {
            if (spawnedItems == null) return;

            foreach (var stack in spawnedItems)
            {
                try
                {
                    if (stack?.itemValue?.ItemClass == null ||
                            stack.itemValue.ModificationCount == 0 ||
                            stack?.itemValue.Quality == 0 ||
                            stack.itemValue.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("noMods")))
                        continue;
                    if (stack.itemValue.TryGetMetadata(AffixesRolledMetadataKey, out float rolled) && rolled > 0)
                        continue;
                    if (stack.itemValue.ItemClass.HasAnyTags(AffixUtils.WeaponTag) ||
                            stack.itemValue.ItemClass.HasAnyTags(AffixUtils.ToolTag) ||
                            stack.itemValue.ItemClass.HasAnyTags(AffixUtils.ArmorTag))
                        AffixSystem.ApplyAffixMods(stack.itemValue, player);
                }
                catch (Exception e)
                {
                    if (WeaponAffixesProject.RamDebug.Enabled)
                        Log.Out($"Failed to properly check stack for item with expection: '{e}'");
                }
            }
        }

        internal static void ApplyUniqueAffixes(ItemValue itemValue)
        {
            int count = 0;
            List<string> tags = itemValue.ItemClass.ItemTags.GetTagNames();
            List<string> modsToApply = new List<string>();
            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Contains("affixMod") || tags[i].Contains("uniqueAffix"))
                {
                    modsToApply.Add(tags[i]);
                    count++;
                }
            }

            var newCosmeticModsList = new ItemValue[count];
            for (int i = 0; i < count; i++)
                newCosmeticModsList[i] = ItemClassModifier.GetItem(modsToApply[i]);
            itemValue.SetCosmeticMods(newCosmeticModsList);
        }

        internal static void ApplyAffixMods(ItemValue itemValue, EntityPlayer player)
        {
            if (itemValue == null)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out("ItemValue is null");
                return;
            }

            if (itemValue.ItemClass.HasAnyTags(AffixUtils.UniqueAffixTag))
            {
                if (itemValue.ItemClass.Name != "meleeWpnBladeT1HuntingKnifeUnique")
                    itemValue.Quality = 7;
                ApplyUniqueAffixes(itemValue);
                return;
            }

            // Parse the modlist to see which mods can be added
            List<List<ItemClassModifier>> weaponMods = AffixUtils.GetCorrectModList(itemValue);
            if (!AffixUtils.HasAnyMods(weaponMods))
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out($"No affixes found to apply to {itemValue.ItemClass.Name}. Loaded affixes: {AffixUtils.GetAffixModifiers().Count()}, item tags: {string.Join(",", itemValue.ItemClass.ItemTags.GetTagNames())}");
                return;
            }

            // Check how many mods to add to the weapon
            int toAdd = CountModsToApply(itemValue, player);
            itemValue.SetMetadata(AffixesRolledMetadataKey, 1f);
            // Add random mods
            for (int i = 0; i < toAdd; i++)
            {
                // For each mod to add, first decide on which tier mod to add
                int selectedTier = AffixUtils.RandomizeTierWithOdds(itemValue, player);
                if (!AffixUtils.TrySelectAffixMod(weaponMods, selectedTier, out ItemClassModifier selectedMod))
                    return;

                // Find first empty cosmetic slot
                for (int j = 0; j < itemValue.cosmeticMods.Length; j++)
                {
                    if (itemValue.cosmeticMods[j] == null || itemValue.cosmeticMods[j].IsEmpty())
                    {
                        // Install affix mod into cosmetic slot
                        itemValue.SetCosmeticMod(j, new ItemValue(selectedMod.Id));
                        weaponMods = AffixUtils.RemoveSimilarMods(weaponMods, selectedMod);
                        break;
                    }
                }
                if (!AffixUtils.HasAnyMods(weaponMods)) return;
            }
        }

        internal static int CountModsToApply(ItemValue itemValue, EntityPlayer player)
        {
            if (itemValue == null || itemValue.IsEmpty())
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out("ItemValue is null in countmodstoapply");
                return 0;
            }

            // Find some other way to determine the amount of affixes to apply
            int affixSlots = itemValue.ModificationCount;
            if (affixSlots <= 0)
                affixSlots = 1;
            int maxAffixes = AffixUtils.GetEffectiveMaxAffixes(player);

            int magicFindLvl = AffixUtils.GetProgressionLevel(player, "perkMagicFind");
            if (AffixUtils.ChallengeGroupIsCompleted(player, "ram advanced"))
                affixSlots++;
            if (magicFindLvl > 3 && itemValue.Quality == 6) affixSlots++;
            affixSlots = Math.Min(affixSlots, maxAffixes);

            // Count existing affixes
            int currentAffixes = 0;
            if (itemValue.cosmeticMods != null)
            {
                foreach (var mod in itemValue.cosmeticMods)
                    if (mod?.ItemClass != null && AffixUtils.IsAffixMod(mod.ItemClass)) currentAffixes++;
            }

            int missingAffixes = Math.Max(0, affixSlots - currentAffixes);
            int abundanceLimitedAffixes = RollAffixAbundance(missingAffixes, maxAffixes - currentAffixes);
            int targetSlots = currentAffixes + abundanceLimitedAffixes;

            if (targetSlots > 0 && (itemValue.cosmeticMods == null || itemValue.cosmeticMods.Length < targetSlots))
            {
                var newCosmetic = new ItemValue[targetSlots];
                if (itemValue.cosmeticMods != null)
                    Array.Copy(itemValue.cosmeticMods, newCosmetic, itemValue.cosmeticMods.Length);
                itemValue.SetCosmeticMods(newCosmetic);
            }

            return abundanceLimitedAffixes;
        }

        private static int RollAffixAbundance(int intendedAffixes, int availableSlots)
        {
            if (intendedAffixes <= 0 || availableSlots <= 0)
                return 0;

            int abundance = Math.Max(0, Math.Min(200, AffixUtils.AffixAbundance));
            int affixesToAdd = 0;

            for (int i = 0; i < intendedAffixes && affixesToAdd < availableSlots; i++)
            {
                if (abundance < 100)
                {
                    if (AffixUtils.rng.Next(0, 100) < abundance)
                        affixesToAdd++;
                    continue;
                }

                affixesToAdd++;

                int bonusChance = abundance - 100;
                if (affixesToAdd < availableSlots && bonusChance > 0 && AffixUtils.rng.Next(0, 100) < bonusChance)
                    affixesToAdd++;
            }

            return affixesToAdd;
        }

        internal static bool UpgradeAffix(ItemValue itemValue, List<int> canUpgradeSlots, ref string affixName)
        {
            if (WeaponAffixesProject.RamDebug.Enabled)
                Log.Out("We're in UpgradeAffix.");
            // randomly select one of the affixes to upgrade
            if (canUpgradeSlots.Count == 0)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out("No upgrade slots available while trying to upgrade.");
                return false;
            }
            if (itemValue.cosmeticMods.Length == 0)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out("No affixes installed to upgrade");
                return false;
            }

            int randomIndex = canUpgradeSlots[AffixUtils.rng.Next(canUpgradeSlots.Count)];
            ItemClassModifier oldAffix = itemValue.cosmeticMods[randomIndex].ItemClass as ItemClassModifier;
            if (oldAffix == null || !AffixUtils.TryGetAffixTierIndex(oldAffix.Name, out int oldTierIndex))
                return false;

            int oldTier = oldTierIndex + 1;
            if (oldTier >= AffixUtils.MaxNormallyObtainableAffixTier)
                return false;

            string newName = oldAffix.Name.Substring(0, oldAffix.Name.Length - 1) + (oldTier + 1).ToString();

            if (!(ItemClass.GetItemClass(newName) is ItemClassModifier newAffix)) return false;

            affixName = newAffix.localizedName;
            if (oldAffix.Name.Contains("KillStreak")) GameManager.Instance.myEntityPlayerLocal.Buffs.RemoveBuff("buff" + oldAffix.Name);

            // then apply the upgrade
            itemValue.SetCosmeticMod(randomIndex, new ItemValue(newAffix.Id));

            // Upgrade an affix
            AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc2");

            if (oldTier >= 3)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc6");

            if (oldTier == 5)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase1");

            return true;
        }

        internal static bool AddNewAffix(ItemValue itemValue, ref string affixName, bool fromToken, EntityPlayer player = null)
        {
            if (itemValue == null || itemValue.IsEmpty())
                return false;

            List<List<ItemClassModifier>> modList = AffixUtils.GetCorrectModList(itemValue);
            if (!AffixUtils.HasAnyMods(modList)) return false;

            int maxAffixes = AffixUtils.GetEffectiveMaxAffixes(player);
            if (itemValue.cosmeticMods != null && itemValue.cosmeticMods.Length >= maxAffixes)
                return false;

            if (itemValue.cosmeticMods == null || itemValue.cosmeticMods.Length == 0)
            {
                var newCosmetics = new ItemValue[1];
                itemValue.SetCosmeticMods(newCosmetics);
            }

            if (itemValue.cosmeticMods[0] != null && !itemValue.cosmeticMods[0].IsEmpty())
            {
                for (int i = 0; i < itemValue.cosmeticMods.Length; i++)
                {
                    if (itemValue.cosmeticMods[i] == null || itemValue.cosmeticMods[i].IsEmpty()) continue;
                    ItemClassModifier mod = itemValue.cosmeticMods[i].ItemClass as ItemClassModifier;
                    modList = AffixUtils.RemoveSimilarMods(modList, mod);
                }

                int affixSlots = itemValue.cosmeticMods.Length + 1;
                if (affixSlots > maxAffixes)
                    return false;
                var newCosmetic = new ItemValue[affixSlots];
                if (itemValue.cosmeticMods != null && !itemValue.cosmeticMods[0].IsEmpty())
                    Array.Copy(itemValue.cosmeticMods, newCosmetic, itemValue.cosmeticMods.Length);
                itemValue.SetCosmeticMods(newCosmetic);
            }
            int tier = (fromToken && AffixUtils.rng.Next(100) < 2) ? 5 : 0;
            if (!AffixUtils.TrySelectAffixMod(modList, tier, out ItemClassModifier selectedMod))
                return false;
            if (selectedMod == null) return false;
            affixName = selectedMod.localizedName;

            // Find first empty cosmetic slot
            for (int j = 0; j < itemValue.cosmeticMods.Length; j++)
            {
                if (itemValue.cosmeticMods[j] == null ||
                        itemValue.cosmeticMods[j].IsEmpty() ||
                        itemValue.cosmeticMods[j].ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("dye"))
                   )
                {
                    // Install affix mod into cosmetic slot
                    itemValue.SetCosmeticMod(j, new ItemValue(selectedMod.Id));

                    // Unlock an affix
                    if (!fromToken)
                        AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc3");
                    return true;
                }
            }
            return false;
        }

        internal static bool CheckUpgradeUnlockAffix(ItemValue itemValue, ref string affixName, int totalAffixes, int maxUpgrade, EntityPlayer player)
        {
            // Check if you should upgrade
            maxUpgrade = Math.Min(maxUpgrade, AffixUtils.MaxNormallyObtainableAffixTier);

            bool didUpgrade = false;
            if (itemValue.cosmeticMods == null || itemValue.cosmeticMods.Length == 0)
                return totalAffixes > 0 && AffixSystem.AddNewAffix(itemValue, ref affixName, false, player);

            // If an empty slot is available, unlock a new one.
            if (itemValue.cosmeticMods.Length < totalAffixes)
            {
                if (itemValue.cosmeticMods[0] == null || itemValue.cosmeticMods[0].IsEmpty()) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName, false, player);
                else if (!AffixUtils.IsAffixMod(itemValue.cosmeticMods[0].ItemClass)) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName, false, player);
                else if (AffixUtils.rng.Next(0, 100) > AffixUtils.unlockNewAffixChance) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName, false, player);
            }
            if (!didUpgrade)
            {
                // Otherwise upgrade an existing affix.
                // Check if all affixes are max rarity
                List<int> canUpgradeSlots = new List<int>();
                for (int i = 0; i < itemValue.cosmeticMods.Length; i++)
                {
                    if (itemValue.cosmeticMods[i]?.ItemClass == null || !AffixUtils.IsAffixMod(itemValue.cosmeticMods[i].ItemClass))
                        continue;

                    if (AffixUtils.TryGetAffixTierIndex(itemValue.cosmeticMods[i].ItemClass.Name, out int tierIndex) && tierIndex + 1 < maxUpgrade)
                        canUpgradeSlots.Add(i);
                }

                // If no slots can be upgraded, see if you can add a new one instead.
                if (canUpgradeSlots.Count == 0)
                {
                    if (itemValue.cosmeticMods.Length < totalAffixes) didUpgrade = AffixSystem.AddNewAffix(itemValue, ref affixName, false, player);
                    else return didUpgrade;
                }
                else didUpgrade = AffixSystem.UpgradeAffix(itemValue, canUpgradeSlots, ref affixName);
            }

            return didUpgrade;
        }

    }
}
