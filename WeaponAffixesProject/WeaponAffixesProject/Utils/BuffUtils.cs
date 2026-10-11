using System.Collections.Generic;

namespace WeaponAffixesProject
{
    public static class BuffUtils
    {
        public static bool IsPositiveBuff(BuffValue buff)
        {
            string name = buff.BuffName;
            // List of all buffs I consider a positive buff
            if (name == "bufffoodstaminabonus" ||
                name == "buffdrugpainkillers" ||
                name == "buffdrugvitamins" ||
                name == "buffcoffee" ||
                name == "buffblackstrapcoffee" ||
                name == "buffyuccajuicesmoothie" ||
                name == "buffbeer" ||
                name == "buffburntsmoothierun" ||
                name == "buffoasissmoothierun" ||
                name == "bufffrostbitesmoothierun" ||
                name == "buffatomicsmoothierun" ||
                name == "buffmegacrush" ||
                name == "buffredtea" ||
                name == "buffdrunkgrandpasmoonshine" ||
                name == "buffdrunkgrandpasawesomesauce" ||
                name == "buffdrunkgrandpaslearningelixir" ||
                name == "buffpumpkincheesecake" ||
                name == "buffshamchowder" ||
                name == "buffchickennuggets" ||
                name == "buffchickenwings" ||
                name == "buffroadrunner" ||
                name == "buffdrugsteroids" ||
                name == "buffdrugrecog" ||
                name == "buffdrugfortbites" ||
                name == "buffdrugatomjunkies" ||
                name == "buffdrugcovertcats" ||
                name == "buffdrugeyekandy" ||
                name == "buffdrughackers" ||
                name == "buffdrughealthbar" ||
                name == "buffdrugjailbreakers" ||
                name == "buffdrugnerdtats" ||
                name == "buffdrugohshitzdrops" ||
                name == "buffdrugrockbusters" ||
                name == "buffdrugskullcrushers" ||
                name == "buffdrugsugarbutts")
            {
                return true;
            }
            return false;
        }

        public static bool IsNegativeBuff(BuffValue buff)
        {
            string name = buff.BuffName;
            // List of all buffs I consider a negative buff
            if (name == "buffinjurybleeding" ||
                name == "buffinjuryabrasion" ||
                name == "buffinjuryabrasiontreated" ||
                name == "bufflegsprained" ||
                name == "bufflegbroken" ||
                name == "bufflegsplinted" ||
                name == "bufflegcast" ||
                name == "buffarmsprained" ||
                name == "buffarmbroken" ||
                name == "buffarmsplinted" ||
                name == "buffarmcast" ||
                name == "bufffatigued" ||
                name == "bufflaceration" ||
                name == "buffinjuryconcussion" ||
                name == "buffdysenterymain" ||
                name == "buffinfectionmain")
                return true;
            return false;
        }

        public static List<string> GetPositiveBuffs(EntityPlayer player)
        {
            List<string> buffNames = new List<string>();
            foreach (var buff in player.Buffs.ActiveBuffs)
            {
                if (IsPositiveBuff(buff))
                    buffNames.Add(buff.BuffName);
            }
            return buffNames;
        }

        public static List<string> GetNegativeBuffs(EntityPlayer player)
        {
            List<string> buffNames = new List<string>();
            foreach (var buff in player.Buffs.ActiveBuffs)
            {
                if (IsNegativeBuff(buff))
                    buffNames.Add(buff.BuffName);
            }
            return buffNames;
        }
    }
}
