using System.Collections.Generic;

namespace WeaponAffixesProject
{
    public static class AffixIncreaseBuffDuration
    {
        private static readonly int[] DurationIncreaseByTier = { 3, 4, 5, 6, 7, 8, 15 };

        public static void IncreaseBuffDurationAffix(EntityPlayerLocal player, ItemValue heldItem)
        {
            List<int> instances = AffixUtils.GetAllAffixesFromItem(heldItem, "buffduration");

            List<string> positiveBuffs = BuffUtils.GetPositiveBuffs(player);

            if (positiveBuffs.Count == 0)
                return;

            foreach (int entry in instances)
            {
                string selected = "$" + positiveBuffs[AffixUtils.rng.Next(positiveBuffs.Count)] + "duration";
                player.Buffs.SetCustomVar(selected, player.Buffs.GetCustomVar(selected) + GetDurationIncrease(entry));
            }
        }

        private static int GetDurationIncrease(int tier)
        {
            if (tier < 1 || tier > DurationIncreaseByTier.Length)
                return 0;

            return DurationIncreaseByTier[tier - 1];
        }
    }
}
