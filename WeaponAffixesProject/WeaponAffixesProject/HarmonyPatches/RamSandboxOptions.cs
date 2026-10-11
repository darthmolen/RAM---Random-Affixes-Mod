using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace WeaponBuffMod.HarmonyPatches
{
    [HarmonyPatch(typeof(SandboxOptions.SandboxOptionManager), nameof(SandboxOptions.SandboxOptionManager.SetupOptions))]
    public static class RamSandboxOptions
    {
        private const string CategoryName = "RAM";

        private static readonly RamSandboxOptionDefinition[] Definitions =
        {
            new RamSandboxOptionDefinition(
                "MaxAffixes",
                "goMaxAffixes",
                "goMaxAffixesDesc",
                (SandboxOptions.SandboxOptions)648,
                7,
                new[] { 3, 4, 5, 6, 7, 8, 9, 10 },
                "{0}"),
            new RamSandboxOptionDefinition(
                "AffixAbundance",
                "goAffixAbundance",
                "goAffixAbundanceDesc",
                (SandboxOptions.SandboxOptions)649,
                100,
                new[] { 25, 33, 50, 75, 100, 125, 150, 200 },
                "{0}%"),
            new RamSandboxOptionDefinition(
                "KillsToUpgrade",
                "goKillsToUpgrade",
                "goKillsToUpgradeDesc",
                (SandboxOptions.SandboxOptions)650,
                100,
                new[] { 25, 50, 75, 100, 125, 150, 200, 250, 300 },
                "{0}"),
            new RamSandboxOptionDefinition(
                "ToggleKillcounter",
                "goToggleKillcounter",
                "goToggleKillcounterDesc",
                (SandboxOptions.SandboxOptions)651,
                1,
                new[] { 1, 0 },
                "{0}",
                new[] { "goRamOptionShow", "goRamOptionHide" }),
            new RamSandboxOptionDefinition(
                "AffixRarity",
                "goAffixRarity",
                "goAffixRarityDesc",
                (SandboxOptions.SandboxOptions)652,
                100,
                new[] { 25, 50, 100, 150, 200 },
                "{0}",
                new[] { "goRamOptionVeryLow", "goRamOptionLow", "goRamOptionDefault", "goRamOptionHigh", "goRamOptionVeryHigh" }),
            new RamSandboxOptionDefinition(
                "TokenLootAbundance",
                "goTokenLootAbundance",
                "goTokenLootAbundanceDesc",
                (SandboxOptions.SandboxOptions)653,
                100,
                new[] { 0, 25, 35, 50, 65, 75, 85, 100, 125, 150, 200, 300, 400, 500 },
                "{0}%",
                new[] { "goRamOptionNone", "goRamOption25Percent", "goRamOption35Percent", "goRamOption50Percent", "goRamOption65Percent", "goRamOption75Percent", "goRamOption85Percent", "goRamOption100Percent", "goRamOption125Percent", "goRamOption150Percent", "goRamOption200Percent", "goRamOption300Percent", "goRamOption400Percent", "goRamOption500Percent" })
        };

        public static void Postfix(SandboxOptions.SandboxOptionManager __instance)
        {
            EnsureRegistered(__instance);
        }

        public static void EnsureRegistered(SandboxOptions.SandboxOptionManager manager)
        {
            if (manager == null)
            {
                return;
            }

            EnsureRamCategory(manager);

            foreach (var definition in Definitions)
            {
                RegisterOption(manager, definition);
            }
        }

        public static void ReloadPresetsIfManagerAlreadyInitialized()
        {
            try
            {
                if (!SandboxOptions.SandboxOptionManager.HasInstance)
                {
                    return;
                }

                var manager = SandboxOptions.SandboxOptionManager.Current;
                EnsureRegistered(manager);

                if (manager.IsInit)
                {
                    manager.LoadPresets();
                    if (WeaponAffixesProject.RamDebug.Enabled)
                        Log.Out("[WeaponBuffMod] Reloaded sandbox presets after registering RAM options.");
                }
            }
            catch (Exception ex)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Warning($"[WeaponBuffMod] Could not reload sandbox presets after registering RAM options. {ex.Message}");
            }
        }

        public static int GetMaxAffixesValue()
        {
            return GetValue(Definitions[0]);
        }

        public static int GetAffixAbundanceValue()
        {
            return GetValue(Definitions[1]);
        }

        public static int GetKillsToUpgradeValue()
        {
            return GetValue(Definitions[2]);
        }

        public static int GetToggleKillcounterValue()
        {
            return GetValue(Definitions[3]);
        }

        public static int GetAffixRarityValue()
        {
            return GetValue(Definitions[4]);
        }

        public static int GetTokenLootAbundanceValue()
        {
            return GetValue(Definitions[5]);
        }

        private static void RegisterOption(SandboxOptions.SandboxOptionManager manager, RamSandboxOptionDefinition definition)
        {
            if (manager.SandboxOptionsDict.TryGetValue(definition.BackingOption, out var existingOption))
            {
                ConfigureOption(existingOption, definition);
                MoveOptionToRamCategory(manager, existingOption, definition.SortOrder);
                return;
            }

            var option = new SandboxOptions.SandboxOptionInt(
                definition.BackingOption,
                definition.OptionName,
                CategoryName,
                string.Empty,
                definition.DefaultValue,
                true,
                null);

            option.ValueOptions = new SandboxOptions.SandboxOptionValueSetInt
            {
                IntValues = definition.Values,
                DisplayValues = LocalizeDisplayValues(definition.DisplayValueLocalizationKeys),
                DisplayFormat = definition.DisplayFormat
            };
            option.ValueOptions.Init();
            option.SetInt(definition.DefaultValue);
            ConfigureOption(option, definition);

            manager.AddSandboxOption(option);
            MoveOptionToRamCategory(manager, option, definition.SortOrder);
            if (WeaponAffixesProject.RamDebug.Enabled)
                Log.Out($"[WeaponBuffMod] Registered sandbox option {definition.OptionName} in {CategoryName}.");
        }

        private static void EnsureRamCategory(SandboxOptions.SandboxOptionManager manager)
        {
            if (!manager.SandboxOptionCategories.Any(category => category.CategoryName == CategoryName))
            {
                manager.SandboxOptionCategories.Insert(0, new SandboxOptions.SandboxOptionCategory
                {
                    CategoryName = CategoryName,
                    DisplayName = "sandboxOptionCategoryRAM"
                });
            }

            if (manager.OptionsByCategory.Get(CategoryName) == null)
            {
                manager.OptionsByCategory.Add(CategoryName, new List<SandboxOptions.BaseSandboxOption>());
            }
        }

        private static void ConfigureOption(SandboxOptions.BaseSandboxOption option, RamSandboxOptionDefinition definition)
        {
            option.OptionName = definition.OptionName;
            option.CategoryName = CategoryName;
            option.NewUISection = false;
            option.optionNameText = Localization.Get(definition.LocalizationKey);
            option.descriptionText = Localization.Get(definition.DescriptionLocalizationKey);
            if (option.ValueOptions is SandboxOptions.SandboxOptionValueSetInt valueSet)
            {
                valueSet.DisplayValues = LocalizeDisplayValues(definition.DisplayValueLocalizationKeys);
                valueSet.Init();
            }
        }

        private static string[] LocalizeDisplayValues(string[] localizationKeys)
        {
            if (localizationKeys == null)
            {
                return null;
            }

            string[] values = new string[localizationKeys.Length];
            for (int i = 0; i < localizationKeys.Length; i++)
            {
                values[i] = Localization.Get(localizationKeys[i]);
            }
            return values;
        }

        private static void MoveOptionToRamCategory(SandboxOptions.SandboxOptionManager manager, SandboxOptions.BaseSandboxOption option, int sortOrder)
        {
            foreach (var category in manager.SandboxOptionCategories)
            {
                manager.OptionsByCategory.Get(category.CategoryName)?.Remove(option);
            }

            var options = manager.OptionsByCategory.Get(CategoryName);
            if (options == null)
            {
                options = new List<SandboxOptions.BaseSandboxOption>();
                manager.OptionsByCategory.Add(CategoryName, options);
            }

            options.Remove(option);
            var insertIndex = Math.Max(0, Math.Min(sortOrder, options.Count));
            options.Insert(insertIndex, option);
        }

        private static int GetValue(RamSandboxOptionDefinition definition)
        {
            try
            {
                var manager = SandboxOptions.SandboxOptionManager.Current;
                return manager != null && manager.SandboxOptionsDict.ContainsKey(definition.BackingOption)
                    ? SandboxOptions.SandboxOptionManager.GetInt(definition.BackingOption)
                    : definition.DefaultValue;
            }
            catch (Exception ex)
            {
                if (WeaponAffixesProject.RamDebug.Enabled)
                    Log.Warning($"[WeaponBuffMod] Could not read {definition.OptionName}; using default {definition.DefaultValue}. {ex.Message}");
                return definition.DefaultValue;
            }
        }

        private sealed class RamSandboxOptionDefinition
        {
            public RamSandboxOptionDefinition(
                string optionName,
                string localizationKey,
                string descriptionLocalizationKey,
                SandboxOptions.SandboxOptions backingOption,
                int defaultValue,
                int[] values,
                string displayFormat,
                string[] displayValueLocalizationKeys = null)
            {
                OptionName = optionName;
                LocalizationKey = localizationKey;
                DescriptionLocalizationKey = descriptionLocalizationKey;
                BackingOption = backingOption;
                DefaultValue = defaultValue;
                Values = values;
                DisplayFormat = displayFormat;
                DisplayValueLocalizationKeys = displayValueLocalizationKeys;
            }

            public string OptionName { get; }

            public string LocalizationKey { get; }

            public string DescriptionLocalizationKey { get; }

            public SandboxOptions.SandboxOptions BackingOption { get; }

            public int DefaultValue { get; }

            public int[] Values { get; }

            public string DisplayFormat { get; }

            public string[] DisplayValueLocalizationKeys { get; }

            public int SortOrder => Array.FindIndex(Definitions, definition => definition.OptionName == OptionName);
        }
    }

    [HarmonyPatch(typeof(XUiC_SandboxOptions), "setupOptions")]
    public static class RamSandboxOptionsUiPatch
    {
        public static void Prefix()
        {
            if (SandboxOptions.SandboxOptionManager.HasInstance)
            {
                RamSandboxOptions.EnsureRegistered(SandboxOptions.SandboxOptionManager.Current);
            }
        }
    }

    [HarmonyPatch(typeof(SandboxOptions.SandboxOptionManager), nameof(SandboxOptions.SandboxOptionManager.LoadPresets))]
    public static class RamSandboxOptionsLoadPresetsPatch
    {
        public static void Prefix(SandboxOptions.SandboxOptionManager __instance)
        {
            RamSandboxOptions.EnsureRegistered(__instance);
        }
    }
}
