using HarmonyLib;

namespace WeaponAffixesProject
{
    [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.OnEntityDeath))]
    public static class EntityAliveOnEntityDeath
    {
        public static void Postfix(EntityAlive __instance)
        {
            if (WeaponAffixesProject.RamDebug.Enabled)
                Log.Out($"'{__instance.LocalizedEntityName}'");
            if (!__instance.HasAnyTags(FastTags<TagGroup.Global>.GetTag("zombie"))) return;

            var player = GameManager.Instance?.myEntityPlayerLocal;
            if (player == null) return;

            ItemValue heldItem = player.inventory?.holdingItemItemValue;
            if (heldItem == null || heldItem.IsEmpty()) return;

            CheckChallengeUpdates(heldItem);

            WeaponUpgrades.OnEntityDeathWeaponUpgrades(__instance, player, heldItem);

            AffixIncreaseBuffDuration.IncreaseBuffDurationAffix(player, heldItem);
        }

        internal static void CheckChallengeUpdates(ItemValue heldItem)
        {
            if (heldItem == null || heldItem.IsEmpty())
                return;

            int totalAffixes = 0;
            int rareAffixes = 0;
            int mythicAffixes = 0;
            CountAffixes(heldItem.cosmeticMods, ref totalAffixes, ref rareAffixes, ref mythicAffixes);
            CountAffixes(heldItem.modifications, ref totalAffixes, ref rareAffixes, ref mythicAffixes);

            // Get kill with any affix
            if (totalAffixes > 0)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc1");

            // Get kill with 3+ affixes
            if (totalAffixes > 2)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc5");

            // Get kill with any rare+ affix
            if (rareAffixes > 0)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamagePerc4");

            // Get kill with 6+ affixes
            if (totalAffixes > 5)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase2");

            // Get kill with 5+ mythical affixes
            if (mythicAffixes > 4)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase4");

            // Get kill with 11+ affixes
            if (totalAffixes > 10)
                AffixUtils.ApplyQuestEventManagerUseItem("affixModEntityDamageBase5");

        }

        private static void CountAffixes(ItemValue[] mods, ref int totalAffixes, ref int rareAffixes, ref int mythicAffixes)
        {
            if (mods == null)
                return;

            foreach (var mod in mods)
            {
                if (mod?.ItemClass == null || mod.IsEmpty() || !AffixUtils.IsAffixMod(mod.ItemClass))
                    continue;

                totalAffixes++;
                if (!AffixUtils.TryGetAffixTierIndex(mod.ItemClass.Name, out int tierIndex))
                    continue;

                if (tierIndex >= 2)
                    rareAffixes++;

                if (tierIndex >= 5)
                    mythicAffixes++;
            }
        }
    }
}
