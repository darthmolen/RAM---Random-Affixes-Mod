using System;
using System.Collections.Generic;
using HarmonyLib;

namespace GamestageBiomeSpawnsProject
{
    public sealed class GamestageBiomeSpawnsApi : IModApi
    {
        public void InitMod(Mod mod)
        {
            Log.Out("[GamestageBiomeSpawns] Initializing 1.0 with MaxEnemyTier support");
            new Harmony("firevos.gamestagebiomespawns").PatchAll();
        }
    }

    internal enum ZombieTier { Normal, Feral, Radiated, Charged, Infernal }

    internal static class WildSpawnGroups
    {
        private static readonly string[] Names =
        {
            "ZombiesAll",
            "ZombiesNight",
            "ZombiesDowntown",
            "ZombiesForestDowntown",
            "ZombiesForestDowntownNight",
            "ZombiesBurntForest",
            "ZombiesBurntForestNight",
            "ZombiesDesert",
            "ZombiesDesertNight",
            "ZombiesSnow",
            "ZombiesSnowNight",
            "ZombiesWasteland",
            "ZombiesAllWasteland",
            "ZombiesWastelandNight",
            "ZombiesWastelandDowntown",
            "ZombiesCrawlers",
            "ZombiesBoss",
            "ZombiesCave"
        };

        public static bool TryFind(int entityClassId, out string groupName)
        {
            for (int groupIndex = 0; groupIndex < Names.Length; groupIndex++)
            {
                string candidateGroup = Names[groupIndex];
                List<SEntityClassAndProb> entries = EntityGroups.list[candidateGroup];
                if (entries == null) continue;

                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    if (entries[entryIndex].entityClassId != entityClassId) continue;
                    groupName = candidateGroup;
                    return true;
                }
            }

            groupName = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(World), nameof(World.SpawnEntityInWorld), new Type[] { typeof(Entity) })]
    internal static class FinalBiomeSpawnPatch
    {
        private static void Prefix(ref Entity _entity)
        {
            if (_entity == null || _entity.GetSpawnerSource() != EnumSpawnerSource.Biome) return;

            int originalClassId = _entity.entityClass;
            string originalName = EntityClass.GetEntityClassName(originalClassId);
            if (string.IsNullOrEmpty(originalName)
                || !originalName.StartsWith("zombie", StringComparison.Ordinal)) return;

            string wildGroup;
            if (!WildSpawnGroups.TryFind(originalClassId, out wildGroup)) return;

            int averageGamestage;
            ZombieTier rolledTier;
            int upgradedClassId = GamestageZombieUpgrader.GetClass(
                originalClassId,
                out averageGamestage,
                out rolledTier);
            string upgradedName = EntityClass.GetEntityClassName(upgradedClassId);
            if (upgradedClassId == originalClassId)
            {
                return;
            }

            Entity originalEntity = _entity;
            Entity replacement = EntityFactory.CreateEntity(
                upgradedClassId,
                originalEntity.entityId,
                originalEntity.position,
                originalEntity.rotation);

            if (replacement == null)
            {
                Log.Warning(
                    "[GamestageBiomeSpawns] Could not replace biome zombie {0} (id {1}) with {2}",
                    originalName,
                    originalEntity.entityId,
                    upgradedName);
                return;
            }

            replacement.SetSpawnerSource(
                originalEntity.GetSpawnerSource(),
                originalEntity.GetSpawnerSourceChunkKey(),
                originalEntity.GetSpawnerSourceBiomeIdHash());

            // EntityFactory already created a Unity object for the original
            // class. Since the final spawn argument is replaced, that object
            // will never enter the world and must be explicitly unloaded and
            // destroyed or its EntityAlive.Update keeps running during exit.
            originalEntity.OnEntityUnload();
            UnityEngine.Object.Destroy(originalEntity.gameObject);

            _entity = replacement;
        }
    }

    internal static class GamestageZombieUpgrader
    {
        private static readonly string[] Suffixes = { "", "Feral", "Radiated", "Charged", "Infernal" };

        public static int GetClass(int originalClassId, out int averageGamestage, out ZombieTier rolledTier)
        {
            string originalName = EntityClass.GetEntityClassName(originalClassId);
            ZombieTier originalTier;
            string baseName = StripTierSuffix(originalName, out originalTier);
            World world = GameManager.Instance?.World;
            averageGamestage = GetAverageGamestage(world);
            rolledTier = world == null ? ZombieTier.Normal : RollTier(averageGamestage, world.GetGameRandom());
            ZombieTier maximumTier = GetMaximumEnemyTier();
            ZombieTier selectedTier = rolledTier > maximumTier ? maximumTier : rolledTier;

            for (ZombieTier tier = selectedTier; tier > originalTier; tier--)
            {
                int candidateId = EntityClass.FromString(baseName + Suffixes[(int)tier]);
                if (EntityClass.list.ContainsKey(candidateId)) return candidateId;
            }

            return originalClassId;
        }

        private static ZombieTier GetMaximumEnemyTier()
        {
            // Vanilla values are 3=Ferals, 4=Radiated and 5=Elites.
            // "Elites" permits both Charged and Infernal zombies.
            if (!SandboxOptions.SandboxOptionManager.HasInstance
                || !SandboxOptions.SandboxOptionManager.Current.IsInit)
                return ZombieTier.Infernal;

            int setting = SandboxOptions.SandboxOptionManager.GetInt(
                SandboxOptions.SandboxOptions.MaxEnemyTier);
            if (setting <= 2) return ZombieTier.Normal;
            if (setting == 3) return ZombieTier.Feral;
            if (setting == 4) return ZombieTier.Radiated;
            return ZombieTier.Infernal;
        }

        private static int GetAverageGamestage(World world)
        {
            if (world == null) return 0;
            List<EntityPlayer> players = world.GetPlayers();
            if (players == null || players.Count == 0) return 0;

            long total = 0;
            int count = 0;
            for (int index = 0; index < players.Count; index++)
            {
                EntityPlayer player = players[index];
                if (player == null || !player.Spawned) continue;
                total += Math.Max(0, player.gameStage);
                count++;
            }

            return count == 0 ? 0 : (int)(total / count);
        }

        private static ZombieTier RollTier(int gamestage, GameRandom random)
        {
            float feral;
            float radiated;
            float charged;
            float infernal;
            GetDistribution(gamestage, out feral, out radiated, out charged, out infernal);

            float roll = random.RandomFloat;
            if (roll < infernal) return ZombieTier.Infernal;
            roll -= infernal;
            if (roll < charged) return ZombieTier.Charged;
            roll -= charged;
            if (roll < radiated) return ZombieTier.Radiated;
            roll -= radiated;
            return roll < feral ? ZombieTier.Feral : ZombieTier.Normal;
        }

        private static void GetDistribution(int gs, out float f, out float r, out float c, out float i)
        {
            if (gs <= 50) { f = r = c = i = 0f; return; }
            if (gs <= 100)
            {
                float t = (gs - 50) / 50f;
                f = Lerp(0f, .10f, t); r = c = i = 0f; return;
            }
            if (gs <= 200)
            {
                float t = (gs - 100) / 100f;
                f = Lerp(.10f, .20f, t); r = Lerp(0f, .05f, t); c = i = 0f; return;
            }
            if (gs <= 500)
            {
                float t = (gs - 200) / 300f;
                f = Lerp(.20f, .30f, t); r = Lerp(.05f, .12f, t);
                c = Lerp(0f, .05f, t); i = c; return;
            }
            if (gs <= 1000)
            {
                float t = (gs - 500) / 500f;
                f = Lerp(.30f, .20f, t); r = Lerp(.12f, .22f, t);
                c = Lerp(.05f, .15f, t); i = c; return;
            }
            if (gs <= 2000)
            {
                float t = (gs - 1000) / 1000f;
                f = Lerp(.20f, .10f, t); r = Lerp(.22f, .30f, t);
                c = Lerp(.15f, .30f, t); i = c; return;
            }
            f = .10f; r = .30f; c = i = .30f;
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * t);
        }

        private static string StripTierSuffix(string name, out ZombieTier tier)
        {
            for (int index = Suffixes.Length - 1; index >= 1; index--)
            {
                string suffix = Suffixes[index];
                if (!name.EndsWith(suffix, StringComparison.Ordinal)) continue;
                tier = (ZombieTier)index;
                return name.Substring(0, name.Length - suffix.Length);
            }

            tier = ZombieTier.Normal;
            return name;
        }
    }
}
