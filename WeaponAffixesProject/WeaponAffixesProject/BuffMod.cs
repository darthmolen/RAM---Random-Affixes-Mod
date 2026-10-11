using System;
using System.Reflection;
using HarmonyLib;
using WeaponAffixesProject;
using WeaponBuffMod.HarmonyPatches;

namespace WeaponBuffMod
{
    public class BuffMod : IModApi
    {
        // PatchAll's default id was the boilerplate "com.example.weaponbuff", which collides
        // with every other mod that kept it.
        private const string HarmonyId = "firevos.randomaffixes";

        public void InitMod(Mod __mod)
        {
            if (RamDebug.Enabled)
                Log.Out("[RAM] Initializing...");

            ApplyPatches();

            CustomSandboxSettings.Register();
            RamSandboxOptions.ReloadPresetsIfManagerAlreadyInitialized();

            if (RamDebug.Enabled)
                Log.Out("[RAM] Initialized.");
        }

        /// <summary>
        /// Patches one class at a time instead of calling PatchAll, which aborts the whole pass on
        /// the first unresolvable target. A single patch broken by a game update then only costs
        /// that one feature, and says so in the log, rather than silently disabling every patch
        /// after it and taking mod init down with it.
        ///
        /// This is the V3.2 failure: one removed method on XUiC_CombineGrid meant
        /// CustomSandboxSettings.Register() never ran and no patch in the assembly applied, while
        /// the XML still loaded and made it look like a config problem.
        /// </summary>
        private static void ApplyPatches()
        {
            Harmony harmony = new Harmony(HarmonyId);
            int applied = 0;
            int skipped = 0;

            foreach (Type type in GetLoadableTypes(Assembly.GetExecutingAssembly()))
            {
                try
                {
                    // Returns null for a class that carries no patches, so the count stays honest.
                    if (harmony.CreateClassProcessor(type).Patch() != null)
                        applied++;
                }
                catch (Exception e)
                {
                    skipped++;
                    Log.Warning($"[RAM] Patch skipped for {type.FullName}: {e.Message}");
                }
            }

            // One line, unconditionally: it is the mod's health signal. "28 patch class(es)
            // applied" in a player's log is what distinguishes "RAM is broken" from "RAM loaded
            // and something else is wrong", and a count that drops after a game update says
            // which it is. A skipped patch means a disabled feature, so it warns.
            if (skipped > 0)
                Log.Warning($"[RAM] {applied} patch class(es) applied, {skipped} skipped — some features are disabled.");
            else
                Log.Out($"[RAM] {applied} patch class(es) applied.");
        }

        /// <summary>
        /// GetTypes() throws ReflectionTypeLoadException outright if any single type fails to
        /// load — which is exactly what a game update removing an API does, and it would abort
        /// the pass before any per-class handler could report it. Keep the types that did load.
        /// </summary>
        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Type[] loaded = Array.FindAll(e.Types, t => t != null);
                Log.Warning($"[RAM] {e.Types.Length - loaded.Length} type(s) failed to load; patching the remaining {loaded.Length}.");
                return loaded;
            }
        }
    }
}
