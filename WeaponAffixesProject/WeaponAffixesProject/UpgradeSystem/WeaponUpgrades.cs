using System;

namespace WeaponAffixesProject
{
    internal static class WeaponUpgrades
    {
        internal const string KillsMetadataKey = "kills";
        internal const string UpgradesMetadataKey = "upgrades";
        internal const string TotalUpgradesMetadataKey = "totalUpgrades";
        internal const string NextUpgradeMetadataKey = "nextUpgrade";
        internal const string LastUpgradeMetadataKey = "lastUpgrade";
        internal const string AscensionsMetadataKey = "ascensions";

        internal static void OnEntityDeathWeaponUpgrades(EntityAlive __instance, EntityPlayerLocal player, ItemValue heldItem)
        {
            int magicSlayerLvl = AffixUtils.GetProgressionLevel(player, "perkMagicSlayer");

            if (!heldItem.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("weapon")) && !heldItem.ItemClass.HasAnyTags(FastTags<TagGroup.Global>.GetTag("tool")))
                return;
            if (!heldItem.TryGetMetadata("kills", out float kills) || !heldItem.TryGetMetadata("upgrades", out float upgrades) || !heldItem.TryGetMetadata("nextUpgrade", out float nextUpgrade) || !heldItem.TryGetMetadata("lastUpgrade", out float lastUpgrade))
                return;

            if (WeaponAffixesProject.RamDebug.Enabled)
                Log.Out($"current kills: {kills}\ncurrent upgrades: {upgrades}\ncurrent nextUpgrade: {nextUpgrade}\ncurrent lastUpgrade: {lastUpgrade}");
            kills += AffixBonusKills.CheckBonusKills(__instance, player, heldItem);
            heldItem.SetMetadata("kills", kills);

            int totalAffixes = AffixUtils.GetEffectiveMaxAffixes(player);
            int maxUpgrade = 4;
            if (AffixUtils.ChallengeGroupIsCompleted(player, "ram basics"))
                maxUpgrade += 2;

            DoUpgradeUnlockCheck(kills, upgrades, lastUpgrade, nextUpgrade, maxUpgrade, totalAffixes, magicSlayerLvl, heldItem, player);

        }

        internal static void DoUpgradeUnlockCheck(float kills, float upgrades, float lastUpgrade, float nextUpgrade, int maxUpgrade, int totalAffixes, int magicSlayerLvl, ItemValue heldItem, EntityPlayerLocal player)
        {
            try
            {
                bool didUpgrade = false;
                string affixName = "";
                int killsToUpgrade = AffixUtils.GetAdjustedKillsToUpgrade(magicSlayerLvl);
                if (nextUpgrade == 0)
                {
                    heldItem.SetMetadata("nextUpgrade", killsToUpgrade * (upgrades + 1));
                    return;
                }
                if (nextUpgrade - lastUpgrade > killsToUpgrade * (upgrades + 1))
                {
                    nextUpgrade = lastUpgrade + killsToUpgrade * (upgrades + 1);
                    heldItem.SetMetadata("nextUpgrade", nextUpgrade);
                }

                if (kills >= nextUpgrade)
                {
                    didUpgrade = AffixSystem.CheckUpgradeUnlockAffix(heldItem, ref affixName, totalAffixes, maxUpgrade, player);
                }
                if (didUpgrade)
                {
                    if (affixName.Contains("Common"))
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixunlock", false), upgrades + 1, killsToUpgrade * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                    else
                        GameManager.ShowTooltip(player, string.Format(Localization.Get("ttaffixup", false), upgrades + 1, killsToUpgrade * (upgrades + 2), affixName), string.Empty, "read_skillbook_final");
                    RecordUpgrade(heldItem, upgrades);
                    heldItem.SetMetadata("nextUpgrade", nextUpgrade + killsToUpgrade * (upgrades + 2));
                    heldItem.SetMetadata("lastUpgrade", kills);
                }
            }
            catch (Exception e)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Out($"'{e}'");
            }
        }

        internal static void RecordUpgrade(ItemValue item, float currentCycleUpgrades)
        {
            if (!item.TryGetMetadata(TotalUpgradesMetadataKey, out float totalUpgrades))
                totalUpgrades = currentCycleUpgrades;

            float newTotalUpgrades = totalUpgrades + 1;
            item.SetMetadata(UpgradesMetadataKey, currentCycleUpgrades + 1);
            item.SetMetadata(TotalUpgradesMetadataKey, newTotalUpgrades);

            if (newTotalUpgrades >= 15)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase3");
        }

        internal static float GetTotalUpgrades(ItemValue item)
        {
            if (item == null)
                return 0;
            if (item.TryGetMetadata(TotalUpgradesMetadataKey, out float totalUpgrades))
                return totalUpgrades;
            return item.TryGetMetadata(UpgradesMetadataKey, out float upgrades) ? upgrades : 0;
        }

        internal static void ResetUpgradeCycleForAscension(ItemValue item, EntityPlayer player)
        {
            item.SetMetadata(TotalUpgradesMetadataKey, GetTotalUpgrades(item));
            item.SetMetadata(KillsMetadataKey, 0f);
            item.SetMetadata(UpgradesMetadataKey, 0f);
            item.SetMetadata(LastUpgradeMetadataKey, 0f);
            item.SetMetadata(NextUpgradeMetadataKey, (float)AffixUtils.GetAdjustedKillsToUpgrade(AffixUtils.GetProgressionLevel(player, "perkMagicSlayer")));
        }

        internal static void SetYOffset(ref int _yOffset)
        {
            if (!IsKillcounterVisible()) return;

            var lp = GameManager.Instance?.myEntityPlayerLocal;
            if (lp == null) return;

            ItemValue held = lp.inventory?.holdingItemItemValue;
            if (held == null || held.IsEmpty()) return;

            bool hasKills = held.TryGetMetadata("kills", out float _);
            bool hasUpg = held.TryGetMetadata("upgrades", out float _);

            if (hasKills || hasUpg) _yOffset += 46;
        }

        internal static bool IsKillcounterVisible()
        {
            return CustomSandboxSettings.GetInt(CustomSandboxSettings.ToggleKillcounter, 1) != 0;
        }
    }
}
