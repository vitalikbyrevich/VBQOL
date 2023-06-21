using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
	internal class CustomSlotItem : MonoBehaviour
	{
		public string m_slotName;
        [HarmonyPatch]
        public class Patches
        {
            [HarmonyPatch(typeof(ItemDrop.ItemData), "IsEquipable")]
            [HarmonyPostfix]
            private static void IsEquipablePostfix(ref bool __result, ref ItemDrop.ItemData __instance)
            {
                __result = (__result || CustomSlotManager.IsCustomSlotItem(__instance));
            }

            [HarmonyPatch(typeof(Humanoid), "Awake")]
            [HarmonyPostfix]
            private static void HumanoidEntryPostfix(ref Humanoid __instance)
            {
                CustomSlotManager.customSlotItemData[__instance] = new Dictionary<string, ItemDrop.ItemData>();
            }

            [HarmonyPatch(typeof(Player), "Load")]
            [HarmonyPostfix]
            private static void InventoryLoadPostfix(ref Player __instance)
            {
                foreach (ItemDrop.ItemData item in __instance.m_inventory.GetEquippedItems())
                {
                    if (CustomSlotManager.IsCustomSlotItem(item))
                    {
                        string customSlotName = CustomSlotManager.GetCustomSlotName(item);
                        CustomSlotManager.SetSlotItem(__instance, customSlotName, item);
                    }
                }
            }

            [HarmonyPatch(typeof(Humanoid), "EquipItem")]
            [HarmonyPostfix]
            private static void EquipItemPostfix(ref bool __result, ref Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
            {
                if (!__result)
                {
                    return;
                }
                if (CustomSlotManager.IsCustomSlotItem(item))
                {
                    string customSlotName = CustomSlotManager.GetCustomSlotName(item);
                    if (CustomSlotManager.IsSlotOccupied(__instance, customSlotName))
                    {
                        __instance.UnequipItem(CustomSlotManager.GetSlotItem(__instance, customSlotName), triggerEquipEffects);
                    }
                    CustomSlotManager.SetSlotItem(__instance, customSlotName, item);
                    if (__instance.IsItemEquiped(item))
                    {
                        item.m_equipped = true;
                    }
                    __instance.SetupEquipment();
                    if (triggerEquipEffects)
                    {
                        __instance.TriggerEquipEffect(item);
                    }
                    __result = true;
                }
            }

            [HarmonyPatch(typeof(Humanoid), "UnequipItem")]
            [HarmonyPostfix]
            private static void UnequipItemPostfix(ref Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
            {
                if (!CustomSlotManager.IsCustomSlotItem(item))
                {
                    return;
                }
                string customSlotName = CustomSlotManager.GetCustomSlotName(item);
                if (item == CustomSlotManager.GetSlotItem(__instance, customSlotName))
                {
                    CustomSlotManager.SetSlotItem(__instance, customSlotName, null);
                }
                __instance.UpdateEquipmentStatusEffects();
            }

            [HarmonyPatch(typeof(Humanoid), "IsItemEquiped")]
            [HarmonyPostfix]
            private static void IsItemEquipedPostfix(ref bool __result, ref Humanoid __instance, ItemDrop.ItemData item)
            {
                if (!CustomSlotManager.IsCustomSlotItem(item))
                {
                    return;
                }
                string customSlotName = CustomSlotManager.GetCustomSlotName(item);
                bool flag = CustomSlotManager.DoesSlotExist(__instance, customSlotName) && CustomSlotManager.GetSlotItem(__instance, customSlotName) == item;
                __result = (__result || flag);
            }

            [HarmonyPatch(typeof(Humanoid), "GetEquipmentWeight")]
            [HarmonyPostfix]
            private static void GetEquipmentWeightPostfix(ref float __result, ref Humanoid __instance)
            {
                foreach (string slotName in CustomSlotManager.customSlotItemData[__instance].Keys)
                {
                    if (CustomSlotManager.IsSlotOccupied(__instance, slotName))
                    {
                        __result += CustomSlotManager.GetSlotItem(__instance, slotName).m_shared.m_weight;
                    }
                }
            }

            [HarmonyPatch(typeof(Humanoid), "UnequipAllItems")]
            [HarmonyPostfix]
            private static void UnequipAllItemsPostfix(ref Humanoid __instance)
            {
                foreach (string slotName in CustomSlotManager.customSlotItemData[__instance].Keys.ToList<string>())
                {
                    if (CustomSlotManager.IsSlotOccupied(__instance, slotName))
                    {
                        __instance.UnequipItem(CustomSlotManager.GetSlotItem(__instance, slotName), false);
                    }
                }
            }

            [HarmonyPatch(typeof(Humanoid), "GetSetCount")]
            [HarmonyPostfix]
            private static void GetSetCountPostfix(ref int __result, ref Humanoid __instance, string setName)
            {
                foreach (string slotName in CustomSlotManager.customSlotItemData[__instance].Keys.ToList<string>())
                {
                    if (CustomSlotManager.IsSlotOccupied(__instance, slotName) && CustomSlotManager.GetSlotItem(__instance, slotName).m_shared.m_setName == setName)
                    {
                        __result++;
                    }
                }
            }

            public static HashSet<StatusEffect> GetStatusEffectsFromCustomSlotItems(Humanoid __instance)
            {
                HashSet<StatusEffect> hashSet = new();
                foreach (string slotName in CustomSlotManager.customSlotItemData[__instance].Keys)
                {
                    if (CustomSlotManager.IsSlotOccupied(__instance, slotName))
                    {
                        if (CustomSlotManager.GetSlotItem(__instance, slotName).m_shared.m_equipStatusEffect)
                        {
                            StatusEffect equipStatusEffect = CustomSlotManager.GetSlotItem(__instance, slotName).m_shared.m_equipStatusEffect;
                            hashSet.Add(equipStatusEffect);
                        }
                        if (__instance.HaveSetEffect(CustomSlotManager.GetSlotItem(__instance, slotName)))
                        {
                            StatusEffect setStatusEffect = CustomSlotManager.GetSlotItem(__instance, slotName).m_shared.m_setStatusEffect;
                            hashSet.Add(setStatusEffect);
                        }
                    }
                }
                return hashSet;
            }

            [HarmonyPatch(typeof(Humanoid), "UpdateEquipmentStatusEffects")]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdateEquipmentStatusEffectsTranspiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList<CodeInstruction>();
                if (codes[0].opcode != OpCodes.Newobj || codes[1].opcode != OpCodes.Stloc_0)
                {
                    throw new Exception("CustomSlotItemLib Transpiler injection point NOT found!! Game has most likely updated and broken this mod!");
                }
                yield return codes[0];
                yield return codes[1];
                yield return new CodeInstruction(OpCodes.Ldloc_0, null);
                yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                yield return CodeInstruction.Call(typeof(Patches), "GetStatusEffectsFromCustomSlotItems", null, null);
                yield return CodeInstruction.Call(typeof(HashSet<StatusEffect>), "UnionWith", null, null);
                int num;
                for (int i = 2; i < codes.Count; i = num + 1)
                {
                    CodeInstruction codeInstruction = codes[i];
                    yield return codeInstruction;
                    num = i;
                }
                yield break;
            }

            [HarmonyPatch]
            public class PatchSlot
            {
                [HarmonyPatch(typeof(ZNetScene), "Awake")]
                [HarmonyPostfix]
                private static void PrefabPostfix(ref ZNetScene __instance)
                {
                    CustomSlotManager.ApplyCustomSlotItem(__instance.GetPrefab("Wishbone"), "wishbone");
                    CustomSlotManager.ApplyCustomSlotItem(__instance.GetPrefab("Demister"), "wisplight");
                }
            }
        }
    }
}
