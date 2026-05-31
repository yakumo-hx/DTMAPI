using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AnimalMap
{
	private readonly Room room;

	private readonly Dictionary<Vector2Int, AnimalStair> _groundStairs;

	private Dictionary<Vector2Int, AnimalStair> _platformStairs;

	private Dictionary<Vector2Int, AnimalStair> _buildingStairs;

	private Dictionary<Vector2Int, AnimalStair> _stairs;

	private Dictionary<Vector2Int, Vector2Int[]> _nearStairs;

	private Dictionary<int, AnimalStair[]> _heightMap;

	public IEnumerable<RectInt> AllAreas
	{
		get
		{
			if (_stairs.IsNullOrEmpty())
			{
				yield break;
			}
			foreach (AnimalStair value in _stairs.Values)
			{
				yield return value.Area;
			}
		}
	}

	public AnimalMap(Room room)
	{
		this.room = room;
		_groundStairs = LoadGroundStairs();
		_platformStairs = LoadPlatformStairs();
		_buildingStairs = LoadBuildingStairs();
		_stairs = ResolveStairs();
	}

	public bool TryFindStair(Vector2Int position, out AnimalStair stair)
	{
		stair = default(AnimalStair);
		if (!_heightMap.TryGetValue(position.y, out var value))
		{
			return false;
		}
		AnimalStair[] array = value;
		for (int i = 0; i < array.Length; i++)
		{
			AnimalStair animalStair = array[i];
			if (animalStair.Contains(position))
			{
				stair = animalStair;
				return true;
			}
		}
		return false;
	}

	public AnimalStair FindStair(Vector2Int position)
	{
		return _stairs.GetValueOrDefault(position);
	}

	public AnimalStair[] TryGetNearStairs(AnimalStair stair, int touchThreshold = 0)
	{
		if (!_nearStairs.TryGetValue(stair.start, out var value))
		{
			return Array.Empty<AnimalStair>();
		}
		return (from id in value
			select _stairs[id] into st
			where st._Touch(stair, touchThreshold)
			select st).ToArray();
	}

	public void Refresh()
	{
		_platformStairs = LoadPlatformStairs();
		_buildingStairs = LoadBuildingStairs();
		_stairs = ResolveStairs();
	}

	private Dictionary<Vector2Int, AnimalStair> ResolveStairs()
	{
		List<AnimalStair> list = new List<AnimalStair>();
		list.AddRange(_groundStairs.Values);
		list.AddRange(_platformStairs.Values);
		list.AddRange(_buildingStairs.Values);
		return CombineAnimalStairs(list.ToArray(), out _nearStairs, out _heightMap).ToDictionary((AnimalStair st) => st.start);
	}

	private Dictionary<Vector2Int, AnimalStair> LoadGroundStairs()
	{
		Dictionary<int, Vector2Int[]> dictionary = room.Geometry.groundPositions.ToHeightGroup();
		Dictionary<Vector2Int, AnimalStair> dictionary2 = new Dictionary<Vector2Int, AnimalStair>();
		foreach (KeyValuePair<int, Vector2Int[]> item in dictionary)
		{
			foreach (AnimalStair item2 in LineToStairs(item.Value))
			{
				dictionary2.Add(item2.start, item2);
			}
		}
		return dictionary2;
	}

	private Dictionary<Vector2Int, AnimalStair> LoadPlatformStairs()
	{
		return room.DM_platform.totalPlatforms.Select(FromPlatform).ToDictionary((AnimalStair pt) => pt.start);
	}

	private Dictionary<Vector2Int, AnimalStair> LoadBuildingStairs()
	{
		return (from stair in room.DM_building.Buildings.SelectMany(FromBuilding)
			where stair.IsValid
			select stair).ToDictionary((AnimalStair stair) => stair.start);
	}

	private static AnimalStair[] CombineAnimalStairs(AnimalStair[] stairs, out Dictionary<Vector2Int, Vector2Int[]> nearStairs, out Dictionary<int, AnimalStair[]> heightMap)
	{
		Dictionary<int, List<AnimalStair>> dictionary = new Dictionary<int, List<AnimalStair>>();
		for (int i = 0; i < stairs.Length; i++)
		{
			AnimalStair item = stairs[i];
			if (!dictionary.TryGetValue(item.start.y, out var value))
			{
				value = new List<AnimalStair>();
				dictionary[item.start.y] = value;
			}
			value.Add(item);
		}
		Dictionary<int, AnimalStair[]> dictionary2 = new Dictionary<int, AnimalStair[]>();
		foreach (KeyValuePair<int, List<AnimalStair>> item2 in dictionary)
		{
			AnimalStair[] objects = RemoveRedundantAsInSameline(item2.Value.ToArray());
			dictionary2[item2.Key] = MergeConnectedObjects(objects);
		}
		nearStairs = CalcNearStairs(dictionary2);
		heightMap = dictionary2;
		List<AnimalStair> list = new List<AnimalStair>();
		foreach (KeyValuePair<int, AnimalStair[]> item3 in dictionary2)
		{
			list.AddRange(item3.Value);
		}
		return list.ToArray();
	}

	private static Dictionary<Vector2Int, Vector2Int[]> CalcNearStairs(Dictionary<int, AnimalStair[]> heightMap)
	{
		if (heightMap.Count <= 1)
		{
			return new Dictionary<Vector2Int, Vector2Int[]>();
		}
		Dictionary<Vector2Int, List<Vector2Int>> dictionary = new Dictionary<Vector2Int, List<Vector2Int>>();
		List<int> list = heightMap.Keys.ToList();
		list.Sort();
		int num = list[0];
		int num2 = list[1];
		if (num2 - num == 1)
		{
			foreach (KeyValuePair<Vector2Int, List<Vector2Int>> item in CalcNearStairs(heightMap[num], heightMap[num2]))
			{
				dictionary.Add(item.Key, item.Value);
			}
		}
		int num3 = list[0];
		int num4 = list.Count - 1;
		for (int i = 1; i < list.Count; i++)
		{
			num = list[i];
			if (i == num4)
			{
				if (num - num3 == 1)
				{
					foreach (KeyValuePair<Vector2Int, List<Vector2Int>> item2 in CalcNearStairs(heightMap[num], heightMap[num3]))
					{
						dictionary.Add(item2.Key, item2.Value);
					}
				}
			}
			else
			{
				List<AnimalStair> list2 = new List<AnimalStair>();
				if (num - num3 == 1)
				{
					list2.AddRange(heightMap[num3]);
				}
				num2 = list[i + 1];
				if (num2 - num == 1)
				{
					list2.AddRange(heightMap[num2]);
				}
				foreach (KeyValuePair<Vector2Int, List<Vector2Int>> item3 in CalcNearStairs(heightMap[num], list2.ToArray()))
				{
					dictionary.Add(item3.Key, item3.Value);
				}
			}
			num3 = num;
		}
		Dictionary<Vector2Int, Vector2Int[]> dictionary2 = new Dictionary<Vector2Int, Vector2Int[]>();
		foreach (KeyValuePair<Vector2Int, List<Vector2Int>> item4 in dictionary)
		{
			dictionary2[item4.Key] = item4.Value.ToArray();
		}
		return dictionary2;
	}

	private static Dictionary<Vector2Int, List<Vector2Int>> CalcNearStairs(AnimalStair[] current, AnimalStair[] other)
	{
		Dictionary<Vector2Int, List<Vector2Int>> dictionary = new Dictionary<Vector2Int, List<Vector2Int>>();
		for (int i = 0; i < current.Length; i++)
		{
			AnimalStair animalStair = current[i];
			for (int j = 0; j < other.Length; j++)
			{
				AnimalStair other2 = other[j];
				if (animalStair._Touch(other2))
				{
					if (!dictionary.TryGetValue(animalStair.start, out var value))
					{
						value = new List<Vector2Int>();
						dictionary[animalStair.start] = value;
					}
					value.Add(other2.start);
				}
			}
		}
		return dictionary;
	}

	public static AnimalStair[] MergeConnectedObjects(AnimalStair[] objects)
	{
		bool flag;
		do
		{
			flag = false;
			List<AnimalStair> list = new List<AnimalStair>();
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 0; i < objects.Length; i++)
			{
				if (hashSet.Contains(i))
				{
					continue;
				}
				AnimalStair item = objects[i];
				bool flag2 = false;
				for (int j = i + 1; j < objects.Length; j++)
				{
					if (!hashSet.Contains(j))
					{
						AnimalStair other = objects[j];
						if (item.IsConnected(other))
						{
							AnimalStair item2 = item.Connect(other);
							list.Add(item2);
							hashSet.Add(i);
							hashSet.Add(j);
							flag2 = true;
							flag = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					list.Add(item);
					hashSet.Add(i);
				}
			}
			objects = list.ToArray();
		}
		while (flag);
		return objects;
	}

	private static AnimalStair[] RemoveRedundantAsInSameline(AnimalStair[] inputArray)
	{
		if (inputArray == null || inputArray.Length == 0)
		{
			return inputArray;
		}
		List<AnimalStair> list = new List<AnimalStair>();
		AnimalStair[] array = inputArray.OrderByDescending((AnimalStair a) => a.length).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			bool flag = false;
			AnimalStair animalStair = array[i];
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].Contains(animalStair))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Add(animalStair);
			}
		}
		return list.ToArray();
	}

	private static AnimalStair FromPlatform(Platform platform)
	{
		return new AnimalStair(new Vector2Int(platform.geometry.Left, platform.geometry.Height + 1), platform.geometry.Width);
	}

	private static IEnumerable<AnimalStair> FromBuilding(Building building)
	{
		foreach (AnimalStair item in FromBuildingFloor(building))
		{
			yield return item;
		}
		foreach (AnimalStair item2 in FromBuildingCeiling(building))
		{
			yield return item2;
		}
	}

	private static IEnumerable<AnimalStair> FromBuildingFloor(Building building)
	{
		Vector2Int[] array = building.proto.FrontFloorPositions.Offset(building.Anchor).ToArray();
		if (array.IsNullOrEmpty())
		{
			yield break;
		}
		Vector2Int vector2Int = new Vector2Int(building.proto.FrontFloorWidth, building.proto.Height);
		if (array.Length == vector2Int.x)
		{
			Vector2Int start = new Vector2Int(building.Anchor.x, building.Anchor.y);
			yield return new AnimalStair(start, vector2Int.x);
			yield break;
		}
		foreach (AnimalStair item in LineToStairs(array))
		{
			yield return item;
		}
	}

	private static IEnumerable<AnimalStair> FromBuildingCeiling(Building building)
	{
		Vector2Int[] array = building.proto.FrontCeilingPositions.Offset(building.Anchor).ToArray();
		if (array.IsNullOrEmpty())
		{
			yield break;
		}
		Vector2Int vector2Int = new Vector2Int(building.proto.FrontCellingWidth, building.proto.Height);
		if (array.Length == vector2Int.x)
		{
			Vector2Int start = new Vector2Int(building.Anchor.x, building.Anchor.y + vector2Int.y);
			yield return new AnimalStair(start, vector2Int.x);
			yield break;
		}
		foreach (AnimalStair item in LineToStairs(array))
		{
			yield return item;
		}
	}

	private static IEnumerable<AnimalStair> LineToStairs(Vector2Int[] positions)
	{
		int num = 0;
		Vector2Int vector2Int = positions[num];
		Vector2Int start = vector2Int;
		int num2 = 0;
		Array.Sort(positions, (Vector2Int a, Vector2Int b) => a.x.CompareTo(b.x));
		for (int i = 1; i < positions.Length; i++)
		{
			Vector2Int nextPos = positions[i];
			if (nextPos.x == vector2Int.x + 1)
			{
				num2++;
			}
			else
			{
				yield return new AnimalStair(start, num2 + 1);
				start = nextPos;
				num2 = 0;
			}
			vector2Int = nextPos;
		}
		if (num2 >= 0)
		{
			yield return new AnimalStair(start, num2 + 1);
		}
	}
}
