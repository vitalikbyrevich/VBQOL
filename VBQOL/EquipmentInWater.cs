using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    public class EquipmentInWater
    {
        public static bool HS_CheckWaterItem(ItemDrop.ItemData item)
        {
            if (!VBQOL.modEnabledUEIWConfig.Value)
                return true;

            if (item == null)
            {
                var player = Player.m_localPlayer;
                if (VBQOL.filterModeConfig.Value == VBQOL.FilterMode.Blacklist)
                {
                    if (player.m_leftItem != null && VBQOL.itemBlacklistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                        player.UnequipItem(player.m_leftItem);

                    if (player.m_rightItem != null && VBQOL.itemBlacklistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                        player.UnequipItem(player.m_rightItem);
                }
                if (VBQOL.filterModeConfig.Value == VBQOL.FilterMode.Whitelist)
                // else
                {
                    if (player.m_leftItem != null && !VBQOL.itemWhitelistStrings.Contains(player.m_leftItem.m_dropPrefab.name))
                        player.UnequipItem(player.m_leftItem);

                    if (player.m_rightItem != null && !VBQOL.itemWhitelistStrings.Contains(player.m_rightItem.m_dropPrefab.name))
                        player.UnequipItem(player.m_rightItem);
                }
                return false;
            }

            return VBQOL.filterModeConfig.Value == VBQOL.FilterMode.Blacklist ? VBQOL.itemBlacklistStrings.Contains(item.m_shared.m_name) : VBQOL.itemWhitelistStrings.Contains(item.m_shared.m_name);
        }

        public static class HS_EquipInWaterPatches
        {
            public static IEnumerable<CodeInstruction> HS_PatchPlayerUpdateWaterCheck(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionList = instructions.ToList();

                for (int i = 202; i <= 207; i++)
                {
                    CodeInstruction instruction = instructionList[i];
                    instruction.opcode = OpCodes.Nop;
                }

                return instructionList;
            }

            public static IEnumerable<CodeInstruction> HS_InjectWaterItemCheck(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = new List<CodeInstruction>(instructions);
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldarg_1),
                    new (OpCodes.Call, typeof(VBQOL).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[29].operand)
                };

                instructionList.InsertRange(33, injectionInstructions);
                return instructionList;
            }

            public static IEnumerable<CodeInstruction> HS_PatchFixedUpdatedWaterCheck(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = new List<CodeInstruction>(instructions);
                var injectionInstructions = new List<CodeInstruction>
                {
                    new (OpCodes.Ldnull),
                    new (OpCodes.Call, typeof(VBQOL).GetMethod("HS_CheckWaterItem")),
                    new (OpCodes.Brfalse_S, instructionList[9].operand)
                };

                instructionList.InsertRange(10, injectionInstructions);
                return instructionList;
            }
        }
    }
}