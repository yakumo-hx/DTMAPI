using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public static class RoomGeneratorUtils
{
	public static Vector2Int[] GetObstacles(this Tilemap tilemap)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < tilemap.size.x; i++)
		{
			for (int j = 0; j < tilemap.size.y; j++)
			{
				Vector3Int position = new Vector3Int(i, j, 0);
				if (tilemap.HasTile(position))
				{
					list.Add(new Vector2Int(i, j));
				}
			}
		}
		return list.ToArray();
	}

	public static Vector2Int[] GetObstacles(this Tilemap tilemap, Vector2Int presetSize, bool shouldCompress = false)
	{
		if (shouldCompress)
		{
			tilemap.CompressBounds();
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < presetSize.x; i++)
		{
			for (int j = 0; j < presetSize.y; j++)
			{
				Vector3Int position = new Vector3Int(i, j, 0);
				if (tilemap.HasTile(position))
				{
					list.Add(new Vector2Int(i, j));
				}
			}
		}
		return list.ToArray();
	}

	public static Vector2Int[] GetObstacles(this Tilemap tilemap, Vector2Int pos, Vector2Int size)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = pos.x; i < pos.x + size.x; i++)
		{
			for (int j = pos.y; j < pos.y + size.y; j++)
			{
				Vector3Int position = new Vector3Int(i, j, 0);
				if (tilemap.HasTile(position))
				{
					list.Add(new Vector2Int(i, j) - pos);
				}
			}
		}
		return list.ToArray();
	}

	public static Vector2Int[] GetObstaclesInHouse(this Tilemap tilemap)
	{
		tilemap.CompressBounds();
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < tilemap.size.x; i++)
		{
			for (int j = 0; j < tilemap.size.y; j++)
			{
				Vector3Int position = new Vector3Int(i, j, 0);
				if (!tilemap.HasTile(position))
				{
					list.Add(new Vector2Int(i, j));
				}
			}
		}
		return list.ToArray();
	}

	public static TerrainConstraintSO GenResourceConstraint(Vector2Int[] groundPositions, Tilemap layerOther, Tilemap layerTree, Vector2Int gridPos)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		List<Vector2Int> list2 = new List<Vector2Int>();
		List<Vector2Int> list3 = new List<Vector2Int>();
		List<Vector2Int> list4 = new List<Vector2Int>();
		List<Vector2Int> list5 = new List<Vector2Int>();
		List<Vector2Int> list6 = new List<Vector2Int>();
		for (int i = 0; i < groundPositions.Length; i++)
		{
			Vector2Int item = groundPositions[i];
			Vector3Int position = new Vector3Int(item.x + gridPos.x, item.y + gridPos.y);
			if (layerTree.HasTile(position))
			{
				list.Add(item);
			}
			if (layerOther.HasTile(position))
			{
				switch (layerOther.GetTile(position).name)
				{
				case "grass":
					list4.Add(item);
					break;
				case "machine":
					list5.Add(item);
					break;
				case "ore":
					list2.Add(item);
					break;
				case "sand":
					list3.Add(item);
					break;
				case "other":
					list6.Add(item);
					list5.Add(item);
					list4.Add(item);
					list2.Add(item);
					break;
				}
			}
		}
		return new TerrainConstraintSO(new List<Vector2Int[]>
		{
			list.ToArray(),
			list2.ToArray(),
			list4.ToArray(),
			list5.ToArray(),
			list6.ToArray(),
			list3.ToArray()
		});
	}

	public static TerrainConstraintSO GenEnvObstacles(Vector2Int[] groundPositions, Tilemap tilemap, Vector2Int gridPos)
	{
		if (groundPositions.IsNullOrEmpty() || tilemap == null)
		{
			return new TerrainConstraintSO(new List<Vector2Int[]>());
		}
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < groundPositions.Length; i++)
		{
			Vector2Int item = groundPositions[i];
			Vector3Int position = new Vector3Int(item.x + gridPos.x, item.y + gridPos.y);
			if (tilemap.HasTile(position) && tilemap.GetTile(position).name == "plant_obstacle")
			{
				list.Add(item);
			}
		}
		return new TerrainConstraintSO(new List<Vector2Int[]> { list.ToArray() });
	}

	public static MonsterGenInfoSO GenMonsterInfo(this Tilemap obstacleMap, Tilemap layerMonster, Vector2Int lb, Vector2Int rt, bool shouldGen)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		List<Vector2Int> list2 = new List<Vector2Int>();
		for (int i = lb.x; i < rt.x; i++)
		{
			for (int j = lb.y; j < rt.y; j++)
			{
				Vector3Int vector3Int = new Vector3Int(i, j, 0);
				if (obstacleMap.HasTile(vector3Int) || !layerMonster.HasTile(vector3Int))
				{
					continue;
				}
				string name = layerMonster.GetTile(vector3Int).name;
				if (!(name == "monster_air"))
				{
					if (name == "monster_ground")
					{
						list2.Add((Vector2Int)vector3Int);
					}
				}
				else
				{
					list.Add((Vector2Int)vector3Int);
				}
			}
		}
		return new MonsterGenInfoSO(GenerateSpawnPoints(list.ToArray(), (Vector2Int p) => obstacleMap.CellToWorld((Vector3Int)p)), GenerateSpawnPoints(list2.ToArray(), (Vector2Int p) => obstacleMap.CellToWorld((Vector3Int)p)), shouldGen);
	}

	private static Vector2PositionList[] GenerateSpawnPoints(Vector2Int[] cellPositions, Func<Vector2Int, Vector2> cellToWorld)
	{
		if (cellPositions == null || cellPositions.Length == 0)
		{
			return null;
		}
		List<Vector2PositionList> list = new List<Vector2PositionList>();
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		Dictionary<Vector2Int, List<Vector2Int>> graph = BuildGraph(cellPositions);
		foreach (Vector2Int vector2Int in cellPositions)
		{
			if (!hashSet.Contains(vector2Int))
			{
				List<Vector2Int> list2 = new List<Vector2Int>();
				BFS(vector2Int, hashSet, list2, graph);
				list.Add(new Vector2PositionList(list2.Select(cellToWorld).ToArray()));
			}
		}
		return list.ToArray();
	}

	private static void BFS(Vector2Int start, HashSet<Vector2Int> visited, List<Vector2Int> group, Dictionary<Vector2Int, List<Vector2Int>> graph)
	{
		Queue<Vector2Int> queue = new Queue<Vector2Int>();
		queue.Enqueue(start);
		while (queue.Count > 0)
		{
			Vector2Int vector2Int = queue.Dequeue();
			if (visited.Contains(vector2Int))
			{
				continue;
			}
			visited.Add(vector2Int);
			group.Add(vector2Int);
			if (!graph.TryGetValue(vector2Int, out var value))
			{
				continue;
			}
			foreach (Vector2Int item in value)
			{
				if (!visited.Contains(item))
				{
					queue.Enqueue(item);
				}
			}
		}
	}

	private static Dictionary<Vector2Int, List<Vector2Int>> BuildGraph(Vector2Int[] points)
	{
		Dictionary<Vector2Int, List<Vector2Int>> dictionary = new Dictionary<Vector2Int, List<Vector2Int>>();
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(points);
		for (int i = 0; i < points.Length; i++)
		{
			Vector2Int key = points[i];
			foreach (Vector2Int item in new List<Vector2Int>
			{
				new Vector2Int(key.x + 1, key.y),
				new Vector2Int(key.x - 1, key.y),
				new Vector2Int(key.x, key.y + 1),
				new Vector2Int(key.x, key.y - 1)
			})
			{
				if (hashSet.Contains(item))
				{
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = new List<Vector2Int>();
					}
					dictionary[key].Add(item);
				}
			}
		}
		return dictionary;
	}

	public static TerrainConstraintSO GenVegetationConstraint(this Tilemap layer, Vector2Int[] groundPositions, Vector2Int offset)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		List<Vector2Int> list2 = new List<Vector2Int>();
		List<Vector2Int> list3 = new List<Vector2Int>();
		List<Vector2Int> list4 = new List<Vector2Int>();
		List<Vector2Int> list5 = new List<Vector2Int>();
		foreach (Vector2Int vector2Int in groundPositions)
		{
			Vector2Int vector2Int2 = vector2Int + offset;
			Vector3Int position = new Vector3Int(vector2Int2.x, vector2Int2.y, 0);
			if (layer.HasTile(position))
			{
				switch (layer.GetTile(position).name)
				{
				case "grass":
					list2.Add(vector2Int);
					break;
				case "vegetation_collect":
					list3.Add(vector2Int);
					break;
				case "vegetation_under_water":
					list4.Add(vector2Int);
					break;
				case "vegetation_shine":
					list5.Add(vector2Int);
					break;
				case "other":
					list.Add(vector2Int);
					list2.Add(vector2Int);
					list3.Add(vector2Int);
					list4.Add(vector2Int);
					list5.Add(vector2Int);
					break;
				}
			}
		}
		return new TerrainConstraintSO(new List<Vector2Int[]>
		{
			list.ToArray(),
			list2.ToArray(),
			list3.ToArray(),
			list4.ToArray(),
			list5.ToArray()
		});
	}

	public static TerrainConstraintSO GenEnvObjectConstraint(this Tilemap controlLayer, Tilemap baseMap, Vector2Int lb, Vector2Int rt)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = lb.x; i < rt.x; i++)
		{
			for (int j = lb.y; j < rt.y; j++)
			{
				Vector3Int vector3Int = new Vector3Int(i, j, 0);
				if (!baseMap.HasTile(vector3Int) && controlLayer.HasTile(vector3Int) && controlLayer.GetTile(vector3Int).name == "env_object_bird")
				{
					list.Add((Vector2Int)vector3Int);
				}
			}
		}
		return new TerrainConstraintSO(new List<Vector2Int[]> { list.ToArray() });
	}

	public static TerrainConstraintSO GenEnvObjectConstraint(this Tilemap controlLayer, Vector2Int lb, Vector2Int rt)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = lb.x; i < rt.x; i++)
		{
			for (int j = lb.y; j < rt.y; j++)
			{
				Vector3Int vector3Int = new Vector3Int(i, j, 0);
				if (controlLayer.HasTile(vector3Int) && controlLayer.GetTile(vector3Int).name == "env_object_bird")
				{
					list.Add((Vector2Int)vector3Int);
				}
			}
		}
		return new TerrainConstraintSO(new List<Vector2Int[]> { list.ToArray() });
	}

	public static ResourceGenInfoSO GenEnvObjectInfo(this Tilemap obstacleMap, Tilemap layerMonster, Vector2Int lb, Vector2Int rt, bool shouldGen)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		if (shouldGen)
		{
			for (int i = lb.x; i < rt.x; i++)
			{
				for (int j = lb.y; j < rt.y; j++)
				{
					Vector3Int vector3Int = new Vector3Int(i, j, 0);
					if (!obstacleMap.HasTile(vector3Int) && layerMonster.HasTile(vector3Int) && layerMonster.GetTile(vector3Int).name == "env_object_bird")
					{
						list.Add((Vector2Int)vector3Int);
					}
				}
			}
		}
		return new ResourceGenInfoSO(new TerrainConstraintSO(new List<Vector2Int[]> { list.ToArray() }), shouldGen);
	}
}
