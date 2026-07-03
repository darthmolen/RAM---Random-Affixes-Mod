using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace MapUnlockProject
{
    public sealed class MapUnlockApi : IModApi
    {
        public void InitMod(Mod mod)
        {
            Log.Out("[MapUnlock] Initializing Craftable Map Unlock 1.0.0");
            new Harmony("firevos.mapunlock").PatchAll();
        }
    }

    internal static class PersonalMapAccess
    {
        private const string UnlockCVar = "mapUnlockEnabled";

        public static bool IsMapEnabled()
        {
            if (World.MapEnabled)
            {
                return true;
            }

            EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
            return player?.Buffs != null
                && player.Buffs.HasCustomVar(UnlockCVar)
                && player.Buffs.GetCustomVar(UnlockCVar) > 0f;
        }
    }

    [HarmonyPatch]
    internal static class PersonalMapAccessPatch
    {
        private static readonly FieldInfo MapEnabledField =
            AccessTools.Field(typeof(World), nameof(World.MapEnabled));

        private static readonly MethodInfo IsMapEnabledMethod =
            AccessTools.Method(typeof(PersonalMapAccess), nameof(PersonalMapAccess.IsMapEnabled));

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(XUiC_PlayersList), "updatePlayersList");
            yield return AccessTools.Method(typeof(XUiC_Radial), "SetupMenuData");

            // The permanent Map input action is wired to this generated callback.
            // It directly checks World.MapEnabled before opening the map window.
            System.Type mapHotkeyClosure =
                AccessTools.TypeByName("PlayerMoveController+<>c__DisplayClass75_0");
            MethodInfo mapHotkeyHandler = AccessTools.Method(mapHotkeyClosure, "<Init>b__12");
            if (mapHotkeyHandler != null)
            {
                yield return mapHotkeyHandler;
            }
            else
            {
                Log.Warning("[MapUnlock] Could not find the map hotkey handler; the M shortcut will remain unavailable.");
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldsfld && Equals(instruction.operand, MapEnabledField))
                {
                    yield return new CodeInstruction(OpCodes.Call, IsMapEnabledMethod)
                        .MoveLabelsFrom(instruction)
                        .MoveBlocksFrom(instruction);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch(typeof(XUiController), "GetBindingValueInternal")]
    internal static class MapEnabledBindingPatch
    {
        private static void Postfix(ref bool __result, ref string _value, string _bindingName)
        {
            if (_bindingName == "is_map_enabled")
            {
                _value = PersonalMapAccess.IsMapEnabled().ToString();
                __result = true;
            }
        }
    }
}
