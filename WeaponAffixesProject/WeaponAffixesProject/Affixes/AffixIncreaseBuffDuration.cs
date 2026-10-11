using System.Collections.Generic;

namespace WeaponAffixesProject
{
    public static class AffixIncreaseBuffDuration
    {
        private static readonly int[] DurationIncreaseByTier = { 3, 4, 5, 6, 7, 8, 15 };

        // Most buffs use $<buff name>Duration. These four use legacy timer names.
        private static readonly Dictionary<string, string> DurationCVarByBuff = new Dictionary<string, string>
        {
            { "buffblackstrapcoffee", "$buffCoffeeBSDuration" },
            { "buffdrunkgrandpasmoonshine", "$buffGrandpasMoonshineDuration" },
            { "buffdrunkgrandpasawesomesauce", "$buffGrandpasAwesomeSauceDuration" },
            { "buffdrunkgrandpaslearningelixir", "$buffGrandpasLearningElixirDuration" }
        };

        public static void IncreaseBuffDurationAffix(EntityPlayerLocal player, ItemValue heldItem)
        {
            List<int> instances = AffixUtils.GetAllAffixesFromItem(heldItem, "buffduration");

            List<string> positiveBuffs = BuffUtils.GetPositiveBuffs(player);

            if (positiveBuffs.Count == 0)
                return;

            foreach (int entry in instances)
            {
                string selected = GetDurationCVar(positiveBuffs[AffixUtils.rng.Next(positiveBuffs.Count)]);
                player.Buffs.SetCustomVar(selected, player.Buffs.GetCustomVar(selected) + GetDurationIncrease(entry));
            }
        }

        private static string GetDurationCVar(string buffName)
        {
            string durationCVar;
            if (DurationCVarByBuff.TryGetValue(buffName, out durationCVar))
                return durationCVar;

            return "$" + buffName + "duration";
        }

        private static int GetDurationIncrease(int tier)
        {
            if (tier < 1 || tier > DurationIncreaseByTier.Length)
                return 0;

            return DurationIncreaseByTier[tier - 1];
        }
    }
}
