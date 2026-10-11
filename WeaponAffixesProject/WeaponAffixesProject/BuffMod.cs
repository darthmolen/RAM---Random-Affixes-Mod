using HarmonyLib;
using WeaponAffixesProject;
using WeaponBuffMod.HarmonyPatches;

namespace WeaponBuffMod
{
    public class BuffMod : IModApi
    {
        public void InitMod(Mod __mod)
        {
            if (WeaponAffixesProject.RamDebug.Enabled)
                Log.Out("[WeaponBuffMod] Initializing...");
            var harmony = new Harmony("com.example.weaponbuff");
            harmony.PatchAll();
            CustomSandboxSettings.Register();
            RamSandboxOptions.ReloadPresetsIfManagerAlreadyInitialized();
        }
    }
}
