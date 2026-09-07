using System;
using System.Reflection;
using HarmonyLib;
using WeaponAffixesProject;
using WeaponBuffMod.HarmonyPatches;

namespace WeaponBuffMod
{
    public class BuffMod : IModApi
    {
        private const string HarmonyId = "firevos.randomaffixes";

        public void InitMod(Mod __mod)
        {
            Log.Out("[RAM] Initializing...");

            ApplyPatches();

            CustomSandboxSettings.Register();
            RamSandboxOptions.ReloadPresetsIfManagerAlreadyInitialized();

            Log.Out("[RAM] Initialized.");
        }

        /// <summary>
        /// Patches one class at a time instead of calling PatchAll, which aborts the whole pass on
        /// the first unresolvable target. A single patch broken by a game update then only costs
        /// that one feature, and says so in the log, rather than silently disabling every patch
        /// after it and taking mod init down with it.
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
                    if (harmony.CreateClassProcessor(type).Patch() != null)
                        applied++;
                }
                catch (Exception e)
                {
                    skipped++;
                    Log.Warning($"[RAM] Patch skipped for {type.FullName}: {e.Message}");
                }
            }

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
