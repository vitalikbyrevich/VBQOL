using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    [HarmonyPatch]
    public class FirePlaceUtilites
    {
        public static Fireplace GetAndCheckFireplace(Player player, bool checkIfBurning)
        {
            GameObject hoverObject = player.GetHoverObject();
            Fireplace fireplace = (hoverObject != null) ? hoverObject.GetComponentInParent<Fireplace>() : null;
            if (fireplace == null)
            {
                return null;
            }
            Fireplace component = fireplace.GetComponent<ZNetView>().GetComponent<Fireplace>();
            if (component == null)
            {
                return null;
            }
            if (checkIfBurning)
            {
                if (!component.IsBurning())
                {
                    return null;
                }
                if (component.m_wet)
                {
                    return null;
                }
            }
            return component;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        public static void PlayerOnSpawned_Patch()
        {
            if (VBQOL.disableTorchesConfig.Value)
            {
                float num = 0f;
                if (EnvMan.instance)
                {
                    num = (float)typeof(EnvMan).GetField("m_smoothDayFraction", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(EnvMan.instance);
                }
                int num2 = (int)(num * 24f);
                VBQOL.timeOfDay = ((num2 < 18 && num2 > 6) ? (VBQOL.timeOfDay = "Day") : (VBQOL.timeOfDay = "Night"));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnvMan), "OnEvening")]
        public static void EvnManOnEvening_Patch(Heightmap.Biome biome, EnvSetup currentEnv)
        {
            if (VBQOL.disableTorchesConfig.Value)
            {
                Fireplace[] array = VBQOL.FindObjectsOfType<Fireplace>();
                for (int i = 0; i < array.Length; i++)
                {
                    ZDO zdo = array[i].m_nview.GetZDO();
                    if (zdo == null)
                    {
                        return;
                    }
                    if (zdo.GetBool("turnOffBetweenTime", false) && !zdo.GetBool("enabledFire", false))
                    {
                        zdo.Set("enabledFire", true);
                        zdo.Set("fuel", zdo.GetFloat("hiddenFuelAmount", 0f));
                    }
                }
                VBQOL.timeOfDay = "Night";
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnvMan), "OnMorning")]
        public static void EvnManOnMorning_Patch(Heightmap.Biome biome, EnvSetup currentEnv)
        {
            if (VBQOL.disableTorchesConfig.Value)
            {
                Fireplace[] array = VBQOL.FindObjectsOfType<Fireplace>();
                for (int i = 0; i < array.Length; i++)
                {
                    ZDO zdo = array[i].m_nview.GetZDO();
                    if (zdo == null)
                    {
                        return;
                    }
                    if (zdo.GetBool("turnOffBetweenTime", false) && zdo.GetBool("enabledFire", false))
                    {
                        zdo.Set("enabledFire", false);
                        zdo.Set("fuel", 0f);
                    }
                }
                VBQOL.timeOfDay = "Day";
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
        public static void FireplaceUpdateFireplace_Patch(Fireplace __instance)
        {
            ZDO zdo = __instance.m_nview.GetZDO();
            bool @bool = zdo.GetBool("enabledFire", false);
            float @float = zdo.GetFloat("fuel", 0f);
            if (VBQOL.disableTorchesConfig.Value)
            {
                if (!zdo.GetBool("turnOffBetweenTime", false))
                {
                    return;
                }
                if (VBQOL.timeOfDay == "Night")
                {
                    if (!@bool)
                    {
                        zdo.Set("enabledFire", true);
                        zdo.Set("fuel", zdo.GetFloat("hiddenFuelAmount", 0f));
                    }
                }
                else if (@bool)
                {
                    zdo.Set("enabledFire", false);
                    zdo.Set("fuel", 0f);
                }
            }
            if (!@bool)
            {
                if (@float <= 0f)
                {
                    return;
                }
                zdo.Set("enabledFire", true);
                zdo.Set("fuel", zdo.GetFloat("hiddenFuelAmount", 0f) + @float);
            }
            if (zdo.GetFloat("hiddenFuelAmount", 0f) != @float)
            {
                float value = @float;
                zdo.Set("hiddenFuelAmount", value);
            }
            if (zdo.GetFloat("fuel", 0f) > __instance.m_maxFuel)
            {
                zdo.Set("fuel", __instance.m_maxFuel);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fireplace), "RPC_AddFuel")]
        public static void RPC_AddFuel_Patch(long sender, Fireplace __instance)
        {
            ZDO zdo = __instance.m_nview.GetZDO();
            if (VBQOL.timeOfDay == "Day" && zdo.GetBool("turnOffBetweenTime", false))
            {
                zdo.Set("turnOffBetweenTime", false);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fireplace), "Awake")]
        public static void FireplaceAwake_Patch(Fireplace __instance)
        {
            string name = __instance.name;
            if (VBQOL.customBurnTimesConfig.Value)
            {
                foreach (KeyValuePair<string, int> keyValuePair in VBQOL.customBurnDict)
                {
                    if (name.Contains(keyValuePair.Key) && __instance.m_secPerFuel != (float)keyValuePair.Value)
                    {
                        __instance.m_secPerFuel = (float)keyValuePair.Value;
                    }
                }
            }
            if (!VBQOL.torchUseCoalConfig.Value)
            {
                return;
            }
            GameObject prefab = ZNetScene.instance.GetPrefab("Coal");
            if ((name.Contains("groundtorch") && !name.Contains("green")) || name.Contains("walltorch"))
            {
                __instance.m_fuelItem = prefab.GetComponent<ItemDrop>();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fireplace), "GetHoverText")]
        public static string FireplaceGetHoverText_Patch(string __result, Fireplace __instance)
        {
            string text = __result;
            if (__instance == null)
            {
                return text;
            }
            ZDO zdo = __instance.m_nview.GetZDO();
            float @float = zdo.GetFloat("hiddenFuelAmount", 0f);
            if (VBQOL.extinguishItemsConfig.Value && !__instance.IsBurning() && @float > 0f)
            {
                string text2 = (VBQOL.keyPOTextStringConfig.Value != "none") ? VBQOL.keyPOTextStringConfig.Value : VBQOL.keyPOCodeStringConfig.Value;
                int num = (int)__instance.m_maxFuel;
                text = string.Concat(new string[]
                {
                    text.Replace(string.Format("0/{0}", num), string.Format("{0}/{1}", (int)Mathf.Ceil(@float), num)),
                    "\n[<color=yellow><b>",
                    text2,
                    "</b></color>] ",
                    VBQOL.igniteStringConfig.Value
                });
            }
            if (VBQOL.disableTorchesConfig.Value)
            {
                string text3 = (VBQOL.timeToggleTextStringConfig.Value != "none") ? VBQOL.timeToggleTextStringConfig.Value : VBQOL.timeToggleCodeStringConfig.Value;
                string text4 = zdo.GetBool("turnOffBetweenTime", false) ? VBQOL.timeToggleOffStringConfig.Value : VBQOL.timeToggleStringConfig.Value;
                text = string.Concat(new string[]
                {
                    text,
                    "\n[<color=yellow><b>",
                    text3,
                    "</b></color>] ",
                    text4
                });
            }
            if (!__instance.IsBurning())
            {
                return text;
            }
            if (__instance.m_wet)
            {
                return text;
            }
            if (VBQOL.extinguishItemsConfig.Value)
            {
                string text5 = (VBQOL.keyPOTextStringConfig.Value != "none") ? VBQOL.keyPOTextStringConfig.Value : VBQOL.keyPOCodeStringConfig.Value;
                text = string.Concat(new string[]
                {
                    text,
                    "\n[<color=yellow><b>",
                    text5,
                    "</b></color>] ",
                    VBQOL.extinguishStringConfig.Value
                });
            }
            if (VBQOL.returnFuelConfig.Value)
            {
                string text6 = (VBQOL.returnTextStringConfig.Value != "none") ? VBQOL.returnTextStringConfig.Value : VBQOL.returnCodeStringConfig.Value;
                text = string.Concat(new string[]
                {
                    text,
                    "\n[<color=yellow><b>",
                    text6,
                    "</b></color>] ",
                    VBQOL.returnStringConfig.Value
                });
            }
            if (VBQOL.burnItemsConfig.Value)
            {
                if (!VBQOL.torchBurnConfig.Value)
                {
                    string name = __instance.name;
                    if (name.Contains("groundtorch") || name.Contains("walltorch") || name.Contains("brazier"))
                    {
                        return text;
                    }
                }
                string text7 = (VBQOL.keyBurnTextStringConfig.Value != "none") ? VBQOL.keyBurnTextStringConfig.Value : VBQOL.keyBurnCodeStringConfig.Value;
                text = string.Concat(new string[]
                {
                    text,
                    "\n[<color=yellow><b>",
                    text7,
                    " + 1-8</b></color>] ",
                    VBQOL.burnItemStringConfig.Value
                });
            }
            return text;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Update")]
        public static void PlayerUpdate_Patch(Player __instance)
        {
            if (!__instance)
            {
                return;
            }
            bool key = Input.GetKey(VBQOL.configBurnKey);
            bool keyUp = Input.GetKeyUp(VBQOL.configPOKey);
            bool keyUp2 = Input.GetKeyUp(VBQOL.returnKey);
            if (Input.GetKeyUp(VBQOL.timeToggleKey) && VBQOL.disableTorchesConfig.Value)
            {
                Fireplace andCheckFireplace = GetAndCheckFireplace(__instance, false);
                if (andCheckFireplace == null)
                {
                    return;
                }
                ZDO zdo = andCheckFireplace.m_nview.GetZDO();
                zdo.Set("turnOffBetweenTime", !zdo.GetBool("turnOffBetweenTime", false));
            }
            if (keyUp2 && VBQOL.returnFuelConfig.Value)
            {
                Fireplace andCheckFireplace2 = GetAndCheckFireplace(__instance, true);
                if (andCheckFireplace2 == null)
                {
                    return;
                }
                float num = Mathf.Floor(andCheckFireplace2.m_nview.GetZDO().GetFloat("fuel", 0f));
                GameObject prefab = ZNetScene.instance.GetPrefab(andCheckFireplace2.m_fuelItem.name);
                andCheckFireplace2.m_fuelAddedEffects.Create(andCheckFireplace2.transform.position, andCheckFireplace2.transform.rotation, null, 1f);
                andCheckFireplace2.m_nview.GetZDO().Set("fuel", 0f);
                for (int i = 0; i < (int)num; i++)
                {
                    VBQOL.Instantiate(prefab, andCheckFireplace2.transform.position + Vector3.up, Quaternion.identity).GetComponent<Character>();
                }
            }
            if (keyUp && VBQOL.extinguishItemsConfig.Value)
            {
                Fireplace andCheckFireplace3 = GetAndCheckFireplace(__instance, false);
                if (andCheckFireplace3 == null)
                {
                    return;
                }
                ZDO zdo2 = andCheckFireplace3.m_nview.GetZDO();
                bool flag = !zdo2.GetBool("enabledFire", false);
                zdo2.Set("enabledFire", flag);
                if (!flag)
                {
                    if (VBQOL.timeOfDay == "Night" && zdo2.GetBool("turnOffBetweenTime", false))
                    {
                        zdo2.Set("turnOffBetweenTime", false);
                    }
                    andCheckFireplace3.m_fuelAddedEffects.Create(andCheckFireplace3.transform.position, andCheckFireplace3.transform.rotation, null, 1f);
                    zdo2.Set("fuel", 0f);
                }
                if (flag)
                {
                    if (VBQOL.timeOfDay == "Day" && zdo2.GetBool("turnOffBetweenTime", false))
                    {
                        zdo2.Set("turnOffBetweenTime", false);
                    }
                    andCheckFireplace3.m_fuelAddedEffects.Create(andCheckFireplace3.transform.position, andCheckFireplace3.transform.rotation, null, 1f);
                    zdo2.Set("fuel", zdo2.GetFloat("hiddenFuelAmount", 0f));
                }
            }
            for (int j = 1; j < 9; j++)
            {
                if (key && Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), "Alpha" + j.ToString())) && VBQOL.burnItemsConfig.Value)
                {
                    Fireplace andCheckFireplace4 = GetAndCheckFireplace(__instance, true);
                    if (andCheckFireplace4 == null)
                    {
                        return;
                    }
                    if (!VBQOL.torchBurnConfig.Value)
                    {
                        string name = andCheckFireplace4.name;
                        if (name.Contains("groundtorch") || name.Contains("walltorch") || name.Contains("brazier"))
                        {
                            return;
                        }
                    }
                    Inventory inventory = __instance.GetInventory();
                    ItemDrop.ItemData itemAt = inventory.GetItemAt(j - 1, 0);
                    if (itemAt == null)
                    {
                        return;
                    }
                    if (!VBQOL.notAllowed.Contains(itemAt.m_shared.m_name))
                    {
                        inventory.RemoveOneItem(itemAt);
                        andCheckFireplace4.m_fuelAddedEffects.Create(andCheckFireplace4.transform.position, andCheckFireplace4.transform.rotation, null, 1f);
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "", 0, null);
                        if (itemAt.IsEquipable())
                        {
                            __instance.ToggleEquipped(itemAt);
                        }
                        if (!VBQOL.giveCoalConfig.Value)
                        {
                            return;
                        }
                        GameObject prefab2 = ZNetScene.instance.GetPrefab("Coal");
                        for (int k = 0; k < VBQOL.coalAmountConfig.Value; k++)
                        {
                            VBQOL.Instantiate(prefab2, andCheckFireplace4.transform.position + Vector3.up, Quaternion.identity).GetComponent<Character>();
                        }
                    }
                }
            }
        }
    }
}