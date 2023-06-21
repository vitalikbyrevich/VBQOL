using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    public class QuickTeleport
    {
        public static bool IsQuickTeleportEnabled()
        {
            ConfigEntry<bool> enableMod = VBQOL.enableModQT;
            return enableMod != null && enableMod.Value;
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        private static class UpdateTeleportPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList<CodeInstruction>();
                int num;
                for (int i = 0; i < il.Count; i = num + 1)
                {
                    if (il[i].opcode == OpCodes.Ldc_R4 && il[i].OperandIs(2))
                    {
                        il[i].opcode = OpCodes.Call;
                        il[i].operand = AccessTools.DeclaredMethod(typeof(UpdateTeleportPatch), "Set2SecDelayToZero", null, null);
                    }
                    yield return il[i];
                    if (i > 3 && il[i - 3].opcode == OpCodes.Ldc_R4 && il[i - 3].OperandIs(8))
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(VBQOL), "IsQuickTeleportEnabled", null, null));
                        yield return new CodeInstruction(OpCodes.Not, null);
                        yield return new CodeInstruction(OpCodes.And, null);
                    }
                    num = i;
                }
                yield break;
            }

            private static float Set2SecDelayToZero()
            {
                if (!IsQuickTeleportEnabled())
                {
                    return 2f;
                }
                return 0f;
            }
        }
    }
}