using System.Collections.Generic;
using DolocTown.Config;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class AutomateSystemLocker
{
	private readonly Room rootRoom;

	private readonly AutomateLocker<Equipment> equipmentLocker = new AutomateLocker<Equipment>();

	private readonly AutomateLocker<DropItemBase> dropItemLocker = new AutomateLocker<DropItemBase>();

	private readonly HashSet<DropItemBase> lockedDropItems = new HashSet<DropItemBase>();

	private readonly Dictionary<Equipment, AutomateBot> lockedEquipments = new Dictionary<Equipment, AutomateBot>();

	public AutomateSystemLocker(Room room)
	{
		rootRoom = room;
	}

	public void Update()
	{
		equipmentLocker.ClearInvalidThings();
		dropItemLocker.ClearInvalidThings();
	}

	public void ClearLockedThingsOfBot(AutomateBot bot)
	{
		equipmentLocker.ClearLockedThingsOfBot(bot);
		dropItemLocker.ClearLockedThingsOfBot(bot);
	}

	public bool IsLocked(DropItemBase item)
	{
		return dropItemLocker.IsLocked(item);
	}

	public bool IsUnlocked(DropItemBase item)
	{
		return !dropItemLocker.IsLocked(item);
	}

	public void LockDropItem(AutomateBot bot, DropItemBase item)
	{
		dropItemLocker.Lock(item, bot);
	}

	public void UnlockDropItem(DropItemBase item)
	{
		dropItemLocker.Unlock(item);
	}

	[Command("is_locked_by_automate", Desc = "检查设备是否被自动化系统锁定")]
	public static bool Command_IsLockedByAutomate()
	{
		Equipment selectedEquipment = DolocAPI.SelectedEquipment;
		if (selectedEquipment == null)
		{
			Debug.LogError("未选中设备");
			return false;
		}
		if (DolocAPI.CurrentRoom.RootRoom.DM_automate.Locker.IsLocked(selectedEquipment))
		{
			Debug.Log("设备 " + selectedEquipment.proto.Id + " 被自动化系统锁定");
			return true;
		}
		Debug.Log("设备 " + selectedEquipment.proto.Id + " 没有被自动化系统锁定");
		return false;
	}

	public void UnlockEquipment(Equipment equipment)
	{
		equipmentLocker.Unlock(equipment);
	}

	public void LockEquipment(AutomateBot sender, Equipment equipment)
	{
		equipmentLocker.Lock(equipment, sender);
	}

	public bool IsLocked(Equipment equipment)
	{
		return equipmentLocker.IsLocked(equipment);
	}

	public bool IsUnlocked(Equipment equipment)
	{
		return !IsLocked(equipment);
	}

	public IEnumerable<Equipment> GetEquipmentsNearStation(AutomateBotStation station, bool includeBuilding = false, bool includeOutdoor = false)
	{
		Room room = station.CurrentRoom;
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
			if (!lockedEquipments.ContainsKey(item) && DolocConfig.Tables.TbRecipeGroup.DataMap.TryGetValue(item.RecipeGroupName, out var value) && !item.IsProcessing && value.RecipeIds.Contains(recipe))
			{
				return item;
			}
		}
		return null;
	}
}
