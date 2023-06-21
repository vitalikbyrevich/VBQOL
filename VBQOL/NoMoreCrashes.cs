using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    public class NoMoreCrashes
    {
        [HarmonyPatch(typeof(ZSteamSocket), "SendQueuedPackages")]
        private static class ZSteamSocket_SendQueuedPackages_Patch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> FixingIssue(IEnumerable<CodeInstruction> code)
            {
                List<CodeInstruction> list = code.ToList<CodeInstruction>();
                for (int i = 0; i < list.Count - 3; i++)
                {
                    bool flag = list[i].opcode == OpCodes.Ldloc_3 && list[i + 1].opcode == OpCodes.Ldc_I4_1 && (list[i + 2].opcode == OpCodes.Bne_Un || list[i + 2].opcode == OpCodes.Bne_Un_S);
                    if (flag)
                    {
                        list[i].opcode = OpCodes.Nop;
                        list[i + 1].opcode = OpCodes.Nop;
                        list[i + 2].opcode = OpCodes.Nop;
                        break;
                    }
                }
                return list;
            }
        }
    }
}