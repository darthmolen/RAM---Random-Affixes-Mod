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

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
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
    }
}
