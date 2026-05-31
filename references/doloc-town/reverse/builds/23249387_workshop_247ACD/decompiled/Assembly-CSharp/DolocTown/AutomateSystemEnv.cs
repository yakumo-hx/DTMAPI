using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using UnityEngine;

namespace DolocTown;

public class AutomateSystemEnv
{
	private readonly Room rootRoom;

	private readonly HashSet<DropItemBase> lockedDropItems = new HashSet<DropItemBase>();

	private readonly HashSet<Equipment> lockedEquipments = new HashSet<Equipment>();

	public IEnumerable<DropItemBase> AllDropItems
	{
		get
		{
			foreach (DropItemBase allData in rootRoom.DM_dropitem.AllDatas)
			{
				yield return allData;
			}
			foreach (Building building in rootRoom.DM_building.Buildings)
			{
				foreach (DropItemBase allData2 in building.room.DM_dropitem.AllDatas)
				{
					yield return allData2;
				}
			}
		}
	}

	public AutomateSystemEnv(Room room)
	{
		rootRoom = room;
	}

	public void Update()
	{
		CheckLockedDropItems();
		CheckEquipments();
	}

	private Room GetRoom(string roomGuid)
	{
		if (string.IsNullOrEmpty(roomGuid))
		{
			return rootRoom;
		}
		if (rootRoom.DM_building.QueryBuildingByRoomTitle(roomGuid, out var building))
		{
			return building.room;
		}
		return null;
	}

	public T GetRoomHost<T>(string roomGuid) where T : class
	{
		Room room = GetRoom(roomGuid);
		if (room == null)
		{
			return null;
		}
		return room as T;
	}

	private void CheckLockedDropItems()
	{
		Queue<DropItemBase> queue = new Queue<DropItemBase>();
		foreach (DropItemBase lockedDropItem in lockedDropItems)
		{
			if (lockedDropItem.index < 0)
			{
				queue.Enqueue(lockedDropItem);
			}
		}
		while (queue.Count > 0)
		{
			lockedDropItems.Remove(queue.Dequeue());
		}
	}

	public IEnumerable<DropItemBase> GetDropItemsNearStation(AutomateBotStation station)
	{
		TemplateRoom room = station.CurrentRoom as TemplateRoom;
		if (room.IsInHouse)
		{
			foreach (DropItemBase allData in room.DM_dropitem.AllDatas)
			{
				yield return allData;
			}
		}
		foreach (DropItemBase allData2 in room.DM_dropitem.AllDatas)
		{
			if (station.IsInArea(allData2.PositionWS))
			{
				yield return allData2;
			}
		}
	}

	public DropItemBase GetAnyDropItemNearStation(AutomateBotStation station, bool locked = true)
	{
		foreach (DropItemBase item in GetDropItemsNearStation(station))
		{
			if (!lockedDropItems.Contains(item) && item.IsItem)
			{
				if (locked)
				{
					lockedDropItems.Add(item);
				}
				return item;
			}
		}
		return null;
	}

	public DropItemBase GetAnyDropItemNearStation(AutomateBotStation station, string mainType, bool locked = true)
	{
		foreach (DropItemBase item in GetDropItemsNearStation(station))
		{
			if (!lockedDropItems.Contains(item) && item.IsItem && DolocAPI.QueryItemProto(item.ItemName, out var proto) && !(proto.MainType.Id != mainType))
			{
				if (locked)
				{
					lockedDropItems.Add(item);
				}
				return item;
			}
		}
		return null;
	}

	public IEnumerable<DropItemBase> GetAllDropItemsInRoom(string roomGuid)
	{
		if (string.IsNullOrEmpty(roomGuid))
		{
			foreach (DropItemBase allData in rootRoom.DM_dropitem.AllDatas)
			{
				yield return allData;
			}
		}
		if (!rootRoom.DM_building.QueryBuildingByRoomTitle(roomGuid, out var building))
		{
			yield break;
		}
		foreach (DropItemBase allData2 in building.room.DM_dropitem.AllDatas)
		{
			yield return allData2;
		}
	}

	public DropItemBase GetAnyDropItem(bool locked = true)
	{
		foreach (DropItemBase allDropItem in AllDropItems)
		{
			if (!lockedDropItems.Contains(allDropItem))
			{
				if (locked)
				{
					lockedDropItems.Add(allDropItem);
				}
				return allDropItem;
			}
		}
		return null;
	}

	public bool HasAnyDropItem()
	{
		return GetAnyDropItem(locked: false) != null;
	}

	public DropItemBase GetAnyDropItem(string roomGuid, bool locked = true)
	{
		foreach (DropItemBase item in GetAllDropItemsInRoom(roomGuid))
		{
			if (!lockedDropItems.Contains(item))
			{
				if (locked)
				{
					lockedDropItems.Add(item);
				}
				return item;
			}
		}
		return null;
	}

	public bool HasAnyDropItem(string roomGuid)
	{
		return GetAnyDropItem(roomGuid, locked: false) != null;
	}

	public DropItemBase GetAnyDropItem(string roomGuid, ItemMainTypeInfo mainType, bool locked = true)
	{
		foreach (DropItemBase item in GetAllDropItemsInRoom(roomGuid))
		{
			if (!lockedDropItems.Contains(item) && DolocAPI.QueryItemProto(item.ItemName, out var proto) && !(proto.MainType.Id != mainType.Id))
			{
				if (locked)
				{
					lockedDropItems.Add(item);
				}
				return item;
			}
		}
		return null;
	}

	public bool HasAnyDropItem(string roomGuid, ItemMainTypeInfo type)
	{
		return GetAnyDropItem(roomGuid, type, locked: false) != null;
	}

	public void RemoveDropItem(DropItemBase item, string roomGuid)
	{
		if (item == null)
		{
			return;
		}
		Room room = GetRoom(roomGuid);
		if (room != null)
		{
			lockedDropItems.Remove(item);
			if (room.IsRenderNow)
			{
				((IDropItemHost)room).RemoveDropItem(item);
			}
			else
			{
				((IDropItemHost)room).RemoveDropItemNoRender(item);
			}
		}
	}

	public void GenerateDropItem(string itemName, Vector2 pos, string roomGuid)
	{
		if (!DolocAPI.QueryItemProto(itemName, out var _))
		{
			return;
		}
		Room room = GetRoom(roomGuid);
		if (room != null)
		{
			if (room.IsRenderNow)
			{
				((IDropItemHost)room).CreateDropItem(itemName, pos, shouldSendMsg: false, 0f);
			}
			else
			{
				((IDropItemHost)room).CreateDropItemNoRender(itemName, pos, shouldSendMsg: false);
			}
		}
	}

	public void CheckEquipments()
	{
		Equipment[] array = lockedEquipments.ToArray();
		foreach (Equipment equipment in array)
		{
			if (equipment == null || equipment.index < 0)
			{
				lockedEquipments.Remove(equipment);
			}
		}
	}

	public void UnlockEquipment(Equipment equipment)
	{
		lockedEquipments.Remove(equipment);
	}

	public void LockEquipment(Equipment equipment)
	{
		if (!lockedEquipments.Contains(equipment))
		{
			lockedEquipments.Add(equipment);
		}
	}

	public IEnumerable<Equipment> GetEquipmentsNearStation(AutomateBotStation station, bool includeBuilding = false, bool includeOutdoor = false)
	{
		TemplateRoom room = station.CurrentRoom as TemplateRoom;
		if (room.IsInHouse)
		{
			foreach (Equipment allEquipment in room.DM_equipment.AllEquipments)
			{
				yield return allEquipment;
			}
			if (includeOutdoor && rootRoom.DM_building.QueryBuildingByRoomTitle(room.Title, out var building))
			{
				foreach (Equipment contentsFromPosition in rootRoom.DM_terrain.GetContentsFromPositions<Equipment>(building.CoveredPositions))
				{
					yield return contentsFromPosition;
				}
			}
		}
		foreach (Equipment item in room.DM_terrain.GetContentsFromArea<Equipment>(station.AreaAnchor, station.AreaSize))
		{
			yield return item;
		}
		if (!includeBuilding)
		{
			yield break;
		}
		foreach (Building item2 in rootRoom.DM_terrain.GetContentsFromArea<Building>(station.AreaAnchor, station.AreaSize))
		{
			foreach (Equipment allEquipment2 in item2.room.DM_equipment.AllEquipments)
			{
				yield return allEquipment2;
			}
		}
	}

	public IEnumerable<T> GetEquipmentsNearStation<T>(AutomateBotStation station, bool includeBuilding = false, bool includeOutdoor = false) where T : Equipment
	{
		foreach (Equipment item in GetEquipmentsNearStation(station, includeBuilding, includeOutdoor))
		{
			if (item is T val)
			{
				yield return val;
			}
		}
	}

	public T GetEquipmentOfTypeNearStation<T>(AutomateBotStation station, bool locked = false, bool includeBuilding = false, bool includeOutdoor = false) where T : Equipment
	{
		foreach (T item in GetEquipmentsNearStation<T>(station, includeBuilding, includeOutdoor))
		{
			if (!lockedEquipments.Contains(item))
			{
				if (locked)
				{
					lockedEquipments.Add(item);
				}
				return item;
			}
		}
		return null;
	}

	public Case GetCaseOfLabelNearStation(AutomateBotStation station, string itemId, bool locked = false, bool includeBuilding = false, bool includeOutdoor = false)
	{
		List<Case> list = GetEquipmentsNearStation<Case>(station, includeBuilding, includeOutdoor).ToList();
		foreach (Case item in list)
		{
			if (!lockedEquipments.Contains(item))
			{
				if (locked)
				{
					lockedEquipments.Add(item);
				}
				if (item.inventory.CanPlaceIn(DolocAPI.GenerateItem(itemId)))
				{
					return item;
				}
			}
		}
		return list.FirstOrDefault((Case container) => container.inventory.CanPlaceIn(DolocAPI.GenerateItem(itemId)));
	}

	public PlantBasin GetMatureCropNearStation(AutomateBotStation station)
	{
		foreach (PlantBasin item in GetEquipmentsNearStation<PlantBasin>(station))
		{
			if (!lockedEquipments.Contains(item) && item.Crop != null && item.Crop.isMature)
			{
				return item;
			}
		}
		return null;
	}

	public PlantBasin GetThirstCropNearStation(AutomateBotStation station, float ratio)
	{
		foreach (PlantBasin item in GetEquipmentsNearStation<PlantBasin>(station))
		{
			if (!lockedEquipments.Contains(item) && item.Supply.WaterRatio <= ratio)
			{
				return item;
			}
		}
		return null;
	}

	public PlantBasin GetUnseededNearStation(AutomateBotStation station, string seedName)
	{
		DolocAPI.QuerySeedProto(seedName, out var proto);
		foreach (PlantBasin item in GetEquipmentsNearStation<PlantBasin>(station))
		{
			if (!lockedEquipments.Contains(item) && item.Crop == null && item.SeedTypeInfo.Id == proto.SeedType)
			{
				return item;
			}
		}
		return null;
	}

	public PlantBasin GetUnFertilizedNearStation(AutomateBotStation station)
	{
		foreach (PlantBasin item in GetEquipmentsNearStation<PlantBasin>(station))
		{
			if (!lockedEquipments.Contains(item) && !item.Supply.IsFertilized)
			{
				return item;
			}
		}
		return null;
	}

	public PlantBasin GetUnProtectedNearStation(AutomateBotStation station)
	{
		foreach (PlantBasin item in GetEquipmentsNearStation<PlantBasin>(station))
		{
			if (!lockedEquipments.Contains(item) && !item.Supply.IsProtected)
			{
				return item;
			}
		}
		return null;
	}

	public PowerGeneratorFuel GetNeedFillingFuelGenerator(AutomateBotStation station, float ratio)
	{
		foreach (PowerGeneratorFuel item in GetEquipmentsNearStation<PowerGeneratorFuel>(station))
		{
			if (item.FuelPercent <= ratio)
			{
				return item;
			}
		}
		return null;
	}

	public Synthesizer GetNeedFillingSynthesizer(AutomateBotStation station, string recipe)
	{
		foreach (Synthesizer item in GetEquipmentsNearStation<Synthesizer>(station))
		{
			if (!lockedEquipments.Contains(item) && DolocConfig.Tables.TbRecipeGroup.DataMap.TryGetValue(item.RecipeGroupName, out var value) && !item.IsProcessing && value.RecipeIds.Contains(recipe))
			{
				return item;
			}
		}
		return null;
	}
}
