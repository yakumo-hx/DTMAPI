using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AutomateStationEnv
{
	public class RoomEnv
	{
		public readonly Room room;

		private readonly bool isFullArea;

		private readonly Vector2Int[] cells;

		private readonly Vector2 areaLB;

		private readonly Vector2 areaRT;

		private readonly List<Vector2Int> cellsCache = new List<Vector2Int>();

		public Equipment[] AllEquipments => _AllEquipments.ToArray();

		private IEnumerable<Equipment> _AllEquipments
		{
			get
			{
				if (isFullArea)
				{
					foreach (Equipment allEquipment in room.DM_equipment.AllEquipments)
					{
						yield return allEquipment;
					}
					yield break;
				}
				cellsCache.AddRange(cells);
				while (cellsCache.Count > 0)
				{
					Vector2Int vector2Int = cellsCache[0];
					cellsCache.Remove(vector2Int);
					Equipment content = room.DM_terrain.GetContent<Equipment>(vector2Int);
					if (content != null)
					{
						Vector2Int[] coveredPositions = content.CoveredPositions;
						foreach (Vector2Int item in coveredPositions)
						{
							cellsCache.Remove(item);
						}
						yield return content;
					}
				}
				cellsCache.Clear();
			}
		}

		public IEnumerable<DropItem> AllDropItems
		{
			get
			{
				if (isFullArea)
				{
					foreach (DropItemBase allData in room.DM_dropitem.AllDatas)
					{
						if (allData is DropItem dropItem)
						{
							yield return dropItem;
						}
					}
					yield break;
				}
				foreach (DropItemBase allData2 in room.DM_dropitem.AllDatas)
				{
					if (allData2 is DropItem { PositionWS: var positionWS } dropItem2 && !(positionWS.x < areaLB.x) && !(positionWS.y < areaLB.y) && !(positionWS.x > areaRT.x) && !(positionWS.y > areaRT.y))
					{
						yield return dropItem2;
					}
				}
			}
		}

		public IEnumerable<Building> AllBuildings
		{
			get
			{
				if (isFullArea)
				{
					yield break;
				}
				cellsCache.AddRange(cells);
				while (cellsCache.Count > 0)
				{
					Vector2Int vector2Int = cellsCache[0];
					cellsCache.Remove(vector2Int);
					Building content = room.DM_terrain.GetContent<Building>(vector2Int);
					if (content != null)
					{
						Vector2Int[] coveredPositions = content.CoveredPositions;
						foreach (Vector2Int item in coveredPositions)
						{
							cellsCache.Remove(item);
						}
						yield return content;
					}
				}
				cellsCache.Clear();
			}
		}

		public IEnumerable<T> GetEquipments<T>()
		{
			return AllEquipments.OfType<T>();
		}

		public IEnumerable<T> GetEquipments<T>(Func<T, bool> predicate)
		{
			return AllEquipments.OfType<T>().Where(predicate);
		}

		public RoomEnv(Room room)
		{
			this.room = room;
			isFullArea = true;
			cells = null;
			areaLB = default(Vector2);
			areaRT = default(Vector2);
		}

		public RoomEnv(Room room, Vector2Int anchor, Vector2Int size)
		{
			this.room = room;
			isFullArea = false;
			cells = size.IterateGrid(anchor).ToArray();
			areaLB = room.Geometry.CalcWorldPosition(anchor, Vector2.zero);
			areaRT = room.Geometry.CalcWorldPosition(anchor + size, Vector2.one);
		}
	}

	public readonly RoomEnv[] roomEnvs;

	public readonly Dictionary<Room, RoomEnv> roomEnvDict;

	public IEnumerable<RoomEnv> BrokenRoomEnvs
	{
		get
		{
			RoomEnv[] array = roomEnvs;
			foreach (RoomEnv roomEnv in array)
			{
				if (!(roomEnv.room is TemplateRoomInHouse templateRoomInHouse) || templateRoomInHouse.Building.IsBroken)
				{
					yield return roomEnv;
				}
			}
		}
	}

	public AutomateStationEnv(AutomateBotStation station)
	{
		roomEnvs = BuildEnvGroup(station);
		roomEnvDict = roomEnvs.ToDictionary((RoomEnv env) => env.room, (RoomEnv env) => env);
	}

	public T GetRandomEquipment<T>(Func<T, bool> predicate)
	{
		List<T> list = new List<T>();
		RoomEnv[] array = roomEnvs;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (T equipment in array[i].GetEquipments<T>())
			{
				if (predicate(equipment))
				{
					list.Add(equipment);
				}
			}
		}
		return list.Choice();
	}

	public T GetEquipment<T>(Func<T, bool> predicate)
	{
		RoomEnv[] array = roomEnvs;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (T equipment in array[i].GetEquipments<T>())
			{
				if (predicate(equipment))
				{
					return equipment;
				}
			}
		}
		return default(T);
	}

	public T GetEquipment<T>(Room highPriorityRoom, Func<T, bool> predicate)
	{
		if (roomEnvDict.TryGetValue(highPriorityRoom, out var value))
		{
			foreach (T equipment in value.GetEquipments<T>())
			{
				if (predicate(equipment))
				{
					return equipment;
				}
			}
		}
		RoomEnv[] array = roomEnvs;
		foreach (RoomEnv roomEnv in array)
		{
			if (roomEnv.room == highPriorityRoom)
			{
				continue;
			}
			foreach (T equipment2 in roomEnv.GetEquipments<T>())
			{
				if (predicate(equipment2))
				{
					return equipment2;
				}
			}
		}
		return default(T);
	}

	public bool TryGetEquipment<T>(Func<T, bool> predicate, out T target)
	{
		RoomEnv[] array = roomEnvs;
		for (int i = 0; i < array.Length; i++)
		{
			foreach (T equipment in array[i].GetEquipments<T>())
			{
				if (predicate(equipment))
				{
					target = equipment;
					return true;
				}
			}
		}
		target = default(T);
		return false;
	}

	public bool TryGetEquipment<T>(Room highPriorityRoom, Func<T, bool> predicate, out T target)
	{
		if (roomEnvDict.TryGetValue(highPriorityRoom, out var value))
		{
			foreach (T equipment in value.GetEquipments<T>())
			{
				if (predicate(equipment))
				{
					target = equipment;
					return true;
				}
			}
		}
		RoomEnv[] array = roomEnvs;
		foreach (RoomEnv roomEnv in array)
		{
			if (roomEnv.room == highPriorityRoom)
			{
				continue;
			}
			foreach (T equipment2 in roomEnv.GetEquipments<T>())
			{
				if (predicate(equipment2))
				{
					target = equipment2;
					return true;
				}
			}
		}
		target = default(T);
		return false;
	}

	private static RoomEnv[] BuildEnvGroup(AutomateBotStation station)
	{
		if (station.CurrentRoom.IsInHouse)
		{
			return new RoomEnv[1]
			{
				new RoomEnv(station.CurrentRoom)
			};
		}
		if (!station.IsLargeStation)
		{
			return new RoomEnv[1]
			{
				new RoomEnv(station.CurrentRoom, station.AreaAnchor, station.AreaSize)
			};
		}
		List<RoomEnv> list = new List<RoomEnv>();
		RoomEnv roomEnv = new RoomEnv(station.CurrentRoom, station.AreaAnchor, station.AreaSize);
		list.Add(roomEnv);
		HashSet<Room> hashSet = new HashSet<Room>();
		foreach (Building allBuilding in roomEnv.AllBuildings)
		{
			if (hashSet.Add(allBuilding.room))
			{
				list.Add(new RoomEnv(allBuilding.room));
			}
		}
		return list.ToArray();
	}
}
