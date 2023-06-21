using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using UnityEngine;

namespace VBQOL
{
    public class AutoFeed
    {
        private void Update()
        {
            if (CheckKeyDown(VBQOL.toggleKeyConfig.Value) && !IgnoreKeyPresses(true))
            {
                VBQOL.isOnConfig.Value = !VBQOL.isOnConfig.Value;
             //   VBQOL.Config.Save();
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, string.Format(VBQOL.toggleStringConfig.Value, VBQOL.isOnConfig.Value), 0, null);
            }

        }
        private static string GetPrefabName(string name)
        {
            char[] anyOf = new char[] { '(', ' ' };
            int num = name.IndexOfAny(anyOf);
            string result;
            if (num >= 0)
                result = name.Substring(0, num);
            else
                result = name;
            return result;
        }

        public static List<Container> GetNearbyContainers(Vector3 center, float range, MonsterAI monsterAI)
        {
            try
            {
                List<Container> containers = new();

                foreach (Collider collider in Physics.OverlapSphere(center, Mathf.Max(range, 0), LayerMask.GetMask(new string[] { "piece" })))
                {
                    Container container = collider.transform.parent?.parent?.gameObject?.GetComponent<Container>();
                    if (container?.GetComponent<ZNetView>()?.IsValid() != true)
                        continue;
                    if ((container.name.StartsWith("piece_chest") || container.name.StartsWith("Container")) && container.GetInventory() != null)
                    {
                        if (VBQOL.requireOnlyFoodConfig.Value)
                        {
                            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
                            {
                                if (!monsterAI.m_consumeItems.Exists(i => i.m_itemData.m_shared.m_name == item.m_shared.m_name))
                                    continue;
                            }
                        }
                        containers.Add(container);
                    }
                }
                containers.OrderBy(c => Vector3.Distance(c.transform.position, center));
                return containers;
            }
            catch
            {
                return new List<Container>();
            }
        }

        [HarmonyPatch(typeof(MonsterAI), "UpdateConsumeItem")]
        static class UpdateConsumeItem_Patch
        {
            static void Postfix(MonsterAI __instance, ZNetView ___m_nview, Character ___m_character, Tameable ___m_tamable, List<ItemDrop> ___m_consumeItems, float dt, bool __result)
            {
                if (!VBQOL.modEnabledAFConfig.Value || !VBQOL.isOnConfig.Value || __result || !___m_character || !___m_nview || !___m_nview.IsOwner() || ___m_tamable == null || !___m_tamable.IsHungry() || ___m_consumeItems == null || ___m_consumeItems.Count == 0)
                    return;

                string name = GetPrefabName(__instance.gameObject.name);

                if (VBQOL.animalDisallowTypesConfig.Value.Split(',').Contains(name))
                {
                    return;
                }

                var nearbyContainers = GetNearbyContainers(___m_character.gameObject.transform.position, VBQOL.containerRangeConfig.Value, __instance);

                using List<ItemDrop>.Enumerator enumerator = __instance.m_consumeItems.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    foreach (Container c in nearbyContainers)
                    {
                        if (Utils.DistanceXZ(c.transform.position, __instance.transform.position) < VBQOL.moveProximityConfig.Value && Mathf.Abs(c.transform.position.y - __instance.transform.position.y) > VBQOL.moveProximityConfig.Value)
                            continue;

                        ItemDrop.ItemData item = c.GetInventory().GetItem(enumerator.Current.m_itemData.m_shared.m_name);
                        if (item != null)
                        {
                            if (VBQOL.feedDisallowTypesConfig.Value.Split(',').Contains(item.m_dropPrefab.name))
                            {
                                continue;
                            }

                            if (Time.time - VBQOL.lastFeed < 0.1)
                            {
                                VBQOL.feedCount++;
                                FeedAnimal(__instance, ___m_tamable, ___m_character, c, item, VBQOL.feedCount * 33);
                            }
                            else
                            {
                                VBQOL.feedCount = 0;
                                VBQOL.lastFeed = Time.time;
                                FeedAnimal(__instance, ___m_tamable, ___m_character, c, item, 0);
                            }
                            return;
                        }
                    }
                }
            }
        }
        public static async void FeedAnimal(MonsterAI monsterAI, Tameable tamable, Character character, Container c, ItemDrop.ItemData item, int delay)
        {
            await Task.Delay(delay);

            if (tamable == null || !tamable.IsHungry())
                return;

            if (VBQOL.requireOnlyFoodConfig.Value)
            {
                foreach (ItemDrop.ItemData temp in c.GetInventory().GetAllItems())
                {
                    if (!monsterAI.m_consumeItems.Exists(i => i.m_itemData.m_shared.m_name == temp.m_shared.m_name))
                        return;
                }
            }


            if (VBQOL.requireMoveConfig.Value)
            {
                //Dbgl($"{monsterAI.gameObject.name} {monsterAI.transform.position} trying to move to {c.transform.position} {Utils.DistanceXZ(monsterAI.transform.position, c.transform.position)}");

                ZoneSystem.instance.GetGroundHeight(c.transform.position, out float ground);

                Vector3 groundTarget = new(c.transform.position.x, ground, c.transform.position.z);

                Traverse traverseAI = Traverse.Create(monsterAI);
                traverseAI.Field("m_lastFindPathTime").SetValue(0);

                if (!traverseAI.Method("MoveTo", new object[] { 0.05f, groundTarget, VBQOL.moveProximityConfig.Value, false }).GetValue<bool>())
                    return;

                if (Mathf.Abs(c.transform.position.y - monsterAI.transform.position.y) > VBQOL.moveProximityConfig.Value)
                    return;

                traverseAI.Method("LookAt", new object[] { c.transform.position }).GetValue();

                if (!traverseAI.Method("IsLookingAt", new object[] { c.transform.position, 90f }).GetValue<bool>())
                    return;


                traverseAI.Field("m_aiStatus").SetValue("Consume item");

                //Dbgl($"{monsterAI.gameObject.name} looking at");
            }

            // Dbgl($"{monsterAI.gameObject.name} {monsterAI.transform.position} consuming {item.m_dropPrefab.name} at {c.transform.position}, distance {Utils.DistanceXZ(monsterAI.transform.position, c.transform.position)}");
            ConsumeItem(item, monsterAI, character);

            c.GetInventory().RemoveItem(item.m_shared.m_name, 1);
            typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
            typeof(Container).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c, new object[] { });
        }

        private static void ConsumeItem(ItemDrop.ItemData item, MonsterAI monsterAI, Character character)
        {
            monsterAI.m_onConsumedItem?.Invoke(null);

            (character as Humanoid).m_consumeItemEffects.Create(character.transform.position, Quaternion.identity, null, 1f, -1);
            Traverse.Create(monsterAI).Field("m_animator").GetValue<ZSyncAnimation>().SetTrigger("consume");
        }

        [HarmonyPatch(typeof(Terminal), "InputText")]
        static class InputText_Patch
        {
            static bool Prefix(Terminal __instance)
            {
                if (!VBQOL.modEnabledAFConfig.Value)
                    return true;
                string text = __instance.m_input.text;
                if (text.ToLower().Equals($"{typeof(VBQOL).Namespace.ToLower()} reset"))
                {
                    VBQOL.self.Config.Reload();
                    VBQOL.self.Config.Save();

                    __instance.AddString(text);
                    __instance.AddString($"{VBQOL.self.Info.Metadata.Name} config reloaded");
                    return false;
                }
                return true;
            }
        }
        public static bool IgnoreKeyPresses(bool extra = false)
        {
            if (!extra)
                return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true;
            return ZNetScene.instance == null || Player.m_localPlayer == null || Minimap.IsOpen() || Console.IsVisible() || TextInput.IsVisible() || ZNet.instance.InPasswordDialog() || Chat.instance?.HasFocus() == true || StoreGui.IsVisible() || InventoryGui.IsVisible() || Menu.IsVisible() || TextViewer.instance?.IsVisible() == true;
        }
        public static bool CheckKeyDown(string value)
        {
            try
            {
                return Input.GetKeyDown(value.ToLower());
            }
            catch
            {
                return false;
            }
        }
        public static bool CheckKeyHeld(string value, bool req = true)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return !req;
            }
        }
    }
}