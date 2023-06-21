using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    public class BlastFurnaceTalesAll
    {
        private static Dictionary<string, ItemDrop> metals = new()
        {
            {
                "$item_copperore",
                null
            },
            {
                "$item_copper",
                null
            },
            {
                "$item_ironscrap",
                null
            },
            {
                "$item_iron",
                null
            },
            {
                "$item_tinore",
                null
            },
            {
                "$item_tin",
                null
            },
            {
                "$item_silverore",
                null
            },
            {
                "$item_silver",
                null
            },
            {
                "$item_copperscrap",
                null
            }
        };
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Smelter), "Awake")]
        private static void BlastFurnacePatch(ref Smelter __instance)
        {
            if (__instance.m_name != "$piece_blastfurnace")
            {
                Debug.Log("Ignored non-blast furnace smelter.");
                return;
            }
            Debug.Log("Found a blast furnace! Applying fix.");
            foreach (ItemDrop itemDrop in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Material, ""))
            {
                if (metals.Keys.Contains(itemDrop.m_itemData.m_shared.m_name))
                {
                    Debug.Log("Adding " + itemDrop.m_itemData.m_shared.m_name + " to list of materials.");
                    metals[itemDrop.m_itemData.m_shared.m_name] = itemDrop;
                }
            }
            foreach (Smelter.ItemConversion item in new List<Smelter.ItemConversion>
            {
                new Smelter.ItemConversion
                {
                    m_from = metals["$item_copperore"],
                    m_to = metals["$item_copper"]
                },
                new Smelter.ItemConversion
                {
                    m_from = metals["$item_tinore"],
                    m_to = metals["$item_tin"]
                },
                new Smelter.ItemConversion
                {
                    m_from = metals["$item_ironscrap"],
                    m_to = metals["$item_iron"]
                },
                new Smelter.ItemConversion
                {
                    m_from = metals["$item_silverore"],
                    m_to = metals["$item_silver"]
                },
                new Smelter.ItemConversion
                {
                    m_from = metals["$item_copperscrap"],
                    m_to = metals["$item_copper"]
                }
            })
            {
                __instance.m_conversion.Add(item);
            }
        }
    }
}