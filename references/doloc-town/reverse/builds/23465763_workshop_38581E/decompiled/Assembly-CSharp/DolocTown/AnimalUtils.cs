using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public static class AnimalUtils
{
	public static readonly AnimalPathFinderManager PathFinderManager = new AnimalPathFinderManager();

	private static EquipmentInfo _chicknestProto;

	private static readonly Vector2Int[][] AroundNeighbours = InitAroundNeighbours();

	private static AnimalMapForBuilding _animalMapForBuilding;

	public static Room _FarmRoom
	{
		get
		{
			if (DolocAPI.IsGameInitialized && DolocAPI.IsDataLoaded)
			{
				return DolocAPI.archiveHandle.MainFarm;
			}
			return null;
		}
	}

	public static EquipmentInfo ChickenNestProto
	{
		get
		{
			if (_chicknestProto == null)
			{
				_chicknestProto = DolocConfig.Tables.TbEquipment.Get("chicken_nest");
				EquipmentInfo chicknestProto = _chicknestProto;
				if (chicknestProto == null || !(chicknestProto.Function is EquipmentFuncChickenNest))
				{
					Debug.LogError("AnimalComponent_ChickenNest.ChickenNestProto: 未找到鸡窝原型");
					return null;
				}
			}
			return _chicknestProto;
		}
	}

	public static AnimalMapForBuilding AnimalMapForBuilding
	{
		get
		{
			if (_animalMapForBuilding != null)
			{
				return _animalMapForBuilding;
			}
			_animalMapForBuilding = new AnimalMapForBuilding(DolocAPI.archiveHandle.MainFarm);
			_animalMapForBuilding.Refresh();
			return _animalMapForBuilding;
		}
	}

	public static bool IsRoomClosed(this Room currentRoom)
	{
		if (currentRoom is TemplateRoomInHouse templateRoomInHouse)
		{
			return templateRoomInHouse.Building.isClosed;
		}
		return false;
	}

	public static bool IsRoomTransitionValid(Room currentRoom, Room anotherRoom)
	{
		Room farmRoom = _FarmRoom;
		if (farmRoom == null)
		{
			return false;
		}
		if (currentRoom == farmRoom || anotherRoom == farmRoom)
		{
			return currentRoom.IsInHouse != anotherRoom.IsInHouse;
		}
		return false;
	}

	public static bool IsOnPlatformOrBuilding(Room room, Vector2Int pos)
	{
		if (!IsOnPlatform(room, pos))
		{
			return IsOnBuilding(room, pos);
		}
		return true;
	}

	public static bool CanArrive(Room room, Vector2Int from, Vector2Int to, int width, out Vector2Int[] path)
	{
		path = null;
		if (room == null)
		{
			return false;
		}
		path = PathFinderManager.FindPath(room, from, to, width);
		path = AnimalPathFinderUtils.OptimizePath(path);
		return !path.IsNullOrEmpty();
	}

	public static bool IsPositionWalkable(Room room, Vector2Int pos, int width)
	{
		return _IsPositionWalkable(room, pos, width);
	}

	private static IEnumerable<Vector2Int> GetAllFencePositions(Room room)
	{
		if (room == null)
		{
			yield break;
		}
		foreach (IAnimalFence item in ((IEquipmentHost)room).AllEquipments.Where((Equipment x) => x is IAnimalFence))
		{
			foreach (Vector2Int fencePosition in item.FencePositions)
			{
				yield return fencePosition;
			}
		}
	}

	public static Vector2Int GetNearestValidPosition(Room room, Vector2Int position, int width)
	{
		Vector2Int[] array;
		for (int i = 1; i < 20; i++)
		{
			array = room.Geometry.Ring(position, i);
			foreach (Vector2Int vector2Int in array)
			{
				if (room.Geometry.Contains(vector2Int) && !(vector2Int == position) && _IsPositionWalkable(room, vector2Int, width))
				{
					return vector2Int;
				}
			}
		}
		int num = int.MaxValue;
		Vector2Int result = position;
		array = room.Geometry.groundPositions;
		foreach (Vector2Int vector2Int2 in array)
		{
			if (!(vector2Int2 == position) && _IsPositionWalkable(room, vector2Int2, width))
			{
				int num2 = vector2Int2.ManhattenDistance(position);
				if (num2 < num)
				{
					num = num2;
					result = vector2Int2;
				}
			}
		}
		return result;
	}

	public static Vector2Int[] _FindBaseAvailablePositions(Room room, Vector2Int startPosition, int maxAvailablePositions = 5000)
	{
		if (room == null || !_IsPositionWalkable(room, startPosition, 1))
		{
			return Array.Empty<Vector2Int>();
		}
		HashSet<Vector2Int> hashSet = GetAllFencePositions(room).ToHashSet();
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		HashSet<Vector2Int> hashSet2 = new HashSet<Vector2Int>();
		queue.Enqueue(startPosition);
		while (queue.Count > 0)
		{
			if (--maxAvailablePositions <= 0)
			{
				Debug.LogWarning("AnimalUtils._FindBaseAvailablePositions: 超过最大可用位置数量，停止搜索。");
				break;
			}
			Vector2Int vector2Int = queue.Dequeue();
			if (!hashSet2.Add(vector2Int))
			{
				continue;
			}
			foreach (Vector2Int item in _GetAroundPositions(vector2Int, 1))
			{
				if (room.Geometry.Contains(item) && !hashSet.Contains(item) && !hashSet2.Contains(item) && _IsPositionWalkable(room, item, 1))
				{
					queue.Enqueue(item);
				}
			}
		}
		return hashSet2.ToArray();
	}

	public static Vector2Int[] FindAvailablePositions(Room room, Vector2Int startPosition, int width, int maxAvailablePositions = 5000, string reason = null)
	{
		if (room == null || width < 1)
		{
			return Array.Empty<Vector2Int>();
		}
		if (!_IsPositionWalkable(room, startPosition, width))
		{
			Debug.LogWarning($"AnimalUtils.FindAvailablePositions: 起始位置\"{startPosition}\"不可行走，返回空数组。");
			return Array.Empty<Vector2Int>();
		}
		Vector2Int[] array = _FindBaseAvailablePositions(room, startPosition, maxAvailablePositions);
		if (width == 1)
		{
			return array;
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(array);
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		HashSet<Vector2Int> hashSet2 = new HashSet<Vector2Int>();
		queue.Enqueue(startPosition);
		while (queue.Count > 0)
		{
			if (--maxAvailablePositions <= 0)
			{
				Debug.LogWarning("AnimalUtils.FindAvailablePositions: 超过最大可用位置数量，停止搜索。");
				break;
			}
			Vector2Int vector2Int = queue.Dequeue();
			if (!hashSet2.Add(vector2Int))
			{
				continue;
			}
			foreach (Vector2Int item in _GetAroundPositions(vector2Int, width))
			{
				if (hashSet.Contains(item) && !hashSet2.Contains(item) && _IsPositionWalkable(room, item, width))
				{
					queue.Enqueue(item);
				}
			}
		}
		return hashSet2.ToArray();
	}

	public static bool TryGetFleePosition(Room room, Animal animal, Vector2Int agentPosition, Vector2Int positionCell, out Vector2Int output, int maxRange = 7)
	{
		output = positionCell;
		Vector2Int vector2Int = new Vector2Int(positionCell.x - maxRange, positionCell.y);
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < maxRange * 2 + animal.width; i++)
		{
			list.Add(new Vector2Int(vector2Int.x + i, vector2Int.y));
		}
		List<Vector2Int> list2 = list.Where((Vector2Int x) => room.Geometry.Contains(x) && HorizontalCheck(animal.positionCell, x)).ToList();
		list2.Sort((Vector2Int a, Vector2Int b) => DistanceWeight(a) - DistanceWeight(b));
		if (list2.Count == 0)
		{
			return false;
		}
		output = list2.Last();
		return true;
		int DistanceWeight(Vector2Int pos)
		{
			return pos.ManhattenDistance(agentPosition) - Math.Sign(pos.x - agentPosition.x);
		}
		bool HorizontalCheck(Vector2Int a, Vector2Int b)
		{
			if (a.y != b.y)
			{
				return false;
			}
			int num = Math.Min(a.x, b.x);
			int num2 = Math.Max(a.x, b.x);
			for (int j = num; j <= num2; j++)
			{
				if (!IsPositionWalkable(room, new Vector2Int(j, a.y), animal.width))
				{
					return false;
				}
			}
			return true;
		}
	}

	private static Vector2Int[][] InitAroundNeighbours()
	{
		List<Vector2Int[]> list = new List<Vector2Int[]>();
		for (int i = 0; i < 4; i++)
		{
			list.Add(InitAroundNeighbours(i + 1));
		}
		return list.ToArray();
	}

	private static Vector2Int[] InitAroundNeighbours(int width)
	{
		width = Mathf.Max(1, width);
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = -1; i <= 1; i++)
		{
			if (i == 0)
			{
				list.Add(new Vector2Int(-1, 0));
				list.Add(new Vector2Int(1, 0));
				continue;
			}
			for (int j = -width; j <= width; j++)
			{
				list.Add(new Vector2Int(j, i));
			}
		}
		return list.ToArray();
	}

	public static IEnumerable<Vector2Int> _GetAroundPositions(Vector2Int position, int width)
	{
		return AroundNeighbours[width - 1].Select((Vector2Int offset) => position + offset);
	}

	private static bool IsOnBuilding(Room room, Vector2Int pos)
	{
		if (room.Type != RoomType.Farm || room.IsInHouse)
		{
			return false;
		}
		return AnimalMapForBuilding.CheckWalkableByBuilding(pos);
	}

	public static bool IsOnPlatform(Room room, Vector2Int pos)
	{
		return room.DM_terrain.IsFilled(new Vector2Int(pos.x, pos.y - 1), TerrainLayerName.PlatformSurface);
	}

	private static bool _IsPositionWalkable(Room room, Vector2Int position)
	{
		if (room.Geometry.Contains(position))
		{
			if (!room.Geometry.IsOnGround(position) && !IsOnPlatform(room, position))
			{
				return IsOnBuilding(room, position);
			}
			return true;
		}
		return false;
	}

	private static bool _IsPositionWalkable([NotNull] Room room, Vector2Int position, int width)
	{
		if (width <= 1)
		{
			return _IsPositionWalkable(room, position);
		}
		for (int i = 0; i < width; i++)
		{
			Vector2Int position2 = new Vector2Int(position.x + i, position.y);
			if (!_IsPositionWalkable(room, position2))
			{
				return false;
			}
		}
		return true;
	}

	public static bool BuildChickenNest(Animal animal, Vector2Int anchor, out Equipment equipment)
	{
		equipment = null;
		if (!animal.IsInHome)
		{
			return false;
		}
		EquipmentInfo equipmentInfo = DolocConfig.Tables.TbEquipment.Get("chicken_nest");
		if (equipmentInfo == null || !(equipmentInfo.Function is EquipmentFuncChickenNest))
		{
			Debug.LogError("AnimalComponent_ChickenNest.BuildChickenNest: 未找到鸡窝原型");
			return false;
		}
		Vector2Int[] cvPositions = equipmentInfo.CoveredPositions(anchor);
		IEnumerable<Vector2Int> gdPositions = equipmentInfo.GroundPositions(anchor);
		IEquipmentHost homeRoom = animal.homeRoom;
		if (!homeRoom.IsEquipmentBuildable(cvPositions, gdPositions))
		{
			return false;
		}
		Vector2 vector = new Vector2((float)anchor.x + (float)equipmentInfo.CoverSize.x * 0.5f, anchor.y) * DolocTransform.TILE_WORLD_SIZE + animal.homeRoom.RoomPosition;
		equipment = (animal.IsRender ? homeRoom.CreateEquipment(vector, anchor, equipmentInfo, turn: false) : homeRoom.CreateEquipmentNoRender(vector, anchor, equipmentInfo, turn: false));
		return equipment != null;
	}

	private static IEnumerable<IAnimalMoodAffector> GetAllMoodAffectors(Room room, IEnumerable<Vector2Int> aroundPositions)
	{
		if (room.IsInHouse)
		{
			return room.DM_equipment.AllEquipments.Where((Equipment x) => x is IAnimalMoodAffector).Cast<IAnimalMoodAffector>();
		}
		HashSet<IAnimalMoodAffector> hashSet = new HashSet<IAnimalMoodAffector>();
		List<IAnimalMoodAffector> list = new List<IAnimalMoodAffector>();
		foreach (Vector2Int aroundPosition in aroundPositions)
		{
			if (((IEquipmentHost)room).GetEquipment(aroundPosition) is IAnimalMoodAffector item && hashSet.Add(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static int GetMoodContribution(Room room, IEnumerable<Vector2Int> positions)
	{
		int value = GetAllMoodAffectors(room, positions).Sum((IAnimalMoodAffector moodAffector) => moodAffector.MoodContribution);
		Vector2Int animalMoodContributionRangeEquipment = DolocAPI.GlobalParameter.AnimalMoodContributionRangeEquipment;
		return Math.Clamp(value, animalMoodContributionRangeEquipment.x, animalMoodContributionRangeEquipment.y);
	}

	public static bool GetTransitionPoint(Room currentRoom, Room nextRoom, out Vector2Int pt)
	{
		pt = default(Vector2Int);
		if (currentRoom == null || nextRoom == null)
		{
			return false;
		}
		if (currentRoom.IsInHouse == nextRoom.IsInHouse)
		{
			return false;
		}
		if (currentRoom.IsInHouse)
		{
			Vector2 defaultEntryPosition = currentRoom.Geometry.DefaultEntryPosition;
			pt = currentRoom.Geometry.CalcCellPosition(defaultEntryPosition);
			return true;
		}
		pt = currentRoom.Geometry.CalcCellPosition(((TemplateRoomInHouse)nextRoom).Building.EntryPosition);
		return true;
	}
}
