using System.Collections.Generic;
using System.Linq;

namespace WeaponAffixesProject
{
    public static class AffixResurgence
    {
        public static void ResurgenceCheck(EntityPlayer __instance, DamageSource _damageSource)
        {
            Entity source = __instance.world.GetEntity(_damageSource.getEntityId());
            if (source == null || !source.HasAnyTags(FastTags<TagGroup.Global>.GetTag("zombie")))
                return;

            if (_damageSource.damageType != EnumDamageTypes.Piercing &&
                _damageSource.damageType != EnumDamageTypes.Crushing &&
                _damageSource.damageType != EnumDamageTypes.Bashing &&
                _damageSource.damageType != EnumDamageTypes.Slashing)
                return;

            if (WeaponAffixesProject.RamDebug.Enabled)
                Log.Out($"Name of damaging entity: {source.LocalizedEntityName}");

            List<int> instances = AffixUtils.GetAllAffixesFromArmor(__instance, "resurgent");
            if (instances.Count == 0)
                return;

            int totalChance = instances.Sum();

            instances.Where(x => x == 7).ToList().ForEach(x => totalChance += 3);

            List<string> allDebuffs = BuffUtils.GetNegativeBuffs(__instance);
            if (allDebuffs.Count == 0)
                return;

            if (AffixUtils.rng.Next(100) < totalChance)
            {
                string randomBuff = allDebuffs[AffixUtils.rng.Next(allDebuffs.Count)];
                __instance.Buffs.RemoveBuff(randomBuff);
            }
        }
    }
}
