using System;
using System.Collections.Generic;
using UnityEngine;

namespace VBQOL
{
	public static class CustomSlotManager
	{
		public static string GetCustomSlotName(ItemDrop.ItemData item)
		{
			return item.m_dropPrefab.GetComponent<CustomSlotItem>().m_slotName;
		}

		public static bool IsCustomSlotItem(ItemDrop.ItemData item)
		{
			return item != null && item.m_dropPrefab && item.m_dropPrefab.GetComponent<CustomSlotItem>();
		}

		public static ItemDrop.ItemData GetPrefabItemData(Humanoid humanoid, string slotName)
		{
			if (!CustomSlotManager.DoesSlotExist(humanoid, slotName))
			{
				return null;
			}
			return CustomSlotManager.customSlotItemData[humanoid][slotName].m_dropPrefab.GetComponent<ItemDrop>().m_itemData;
		}

		public static ItemDrop.ItemData GetSlotItem(Humanoid humanoid, string slotName)
		{
			if (CustomSlotManager.DoesSlotExist(humanoid, slotName))
			{
				return CustomSlotManager.customSlotItemData[humanoid][slotName];
			}
			return null;
		}

		public static void SetSlotItem(Humanoid humanoid, string slotName, ItemDrop.ItemData item)
		{
			CustomSlotManager.customSlotItemData[humanoid][slotName] = item;
		}

		public static bool DoesSlotExist(Humanoid humanoid, string slotName)
		{
			return CustomSlotManager.customSlotItemData[humanoid] != null && CustomSlotManager.customSlotItemData[humanoid].ContainsKey(slotName);
		}

		public static bool IsSlotOccupied(Humanoid humanoid, string slotName)
		{
			return CustomSlotManager.customSlotItemData[humanoid] != null && CustomSlotManager.customSlotItemData[humanoid].ContainsKey(slotName) && CustomSlotManager.customSlotItemData[humanoid][slotName] != null;
		}

		public static void ApplyCustomSlotItem(GameObject prefab, string slotName)
		{
			if (!prefab.GetComponent<CustomSlotItem>())
			{
				prefab.AddComponent<CustomSlotItem>();
			}
			prefab.GetComponent<CustomSlotItem>().m_slotName = slotName;
			prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.None;
		}

		public static readonly Dictionary<Humanoid, Dictionary<string, ItemDrop.ItemData>> customSlotItemData = new Dictionary<Humanoid, Dictionary<string, ItemDrop.ItemData>>();
	}
}
