using System.Collections.Generic;
using RedSaw.AI;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public class ObstacleFinder
{
	private readonly IGameMap gameMap;

	private readonly Dictionary<Vector2Int, Vector2[]> obstacleCache = new Dictionary<Vector2Int, Vector2[]>();

	public ObstacleFinder(IGameMap gameMap)
	{
		this.gameMap = gameMap;
		GenerateObstacleCache();
	}

	private void GenerateObstacleCache()
	{
		obstacleCache.Clear();
		foreach (Vector2Int allObstacle in gameMap.AllObstacles)
		{
			Vector2 vector = new Vector2((float)allObstacle.x * gameMap.CellSize.x, (float)allObstacle.y * gameMap.CellSize.y) + gameMap.RoomPosition;
			Vector2[] value = new Vector2[4]
			{
				vector,
				new Vector2(vector.x, vector.y + gameMap.CellSize.y),
				new Vector2(vector.x + gameMap.CellSize.x, vector.y + gameMap.CellSize.y),
				new Vector2(vector.x + gameMap.CellSize.x, vector.y)
			};
			obstacleCache.Add(allObstacle, value);
		}
	}

	public ContextSteeringObstacle[] GetObstacles(Vector2 scenePos, float radius)
	{
		Vector2Int vector2Int = gameMap.WorldToCell(scenePos);
		List<ContextSteeringObstacle> list = new List<ContextSteeringObstacle>();
		int num = Mathf.CeilToInt(radius / gameMap.CellSize.x);
		for (int i = -num; i < num + 1; i++)
		{
			for (int j = -num; j < num + 1; j++)
			{
				Vector2Int key = vector2Int + new Vector2Int(i, j);
				if (!obstacleCache.TryGetValue(key, out var value))
				{
					continue;
				}
				Vector2[] array = value;
				foreach (Vector2 vector in array)
				{
					Vector2 errorVec = vector - scenePos;
					float num2 = Mathf.Max(0.01f, errorVec.magnitude);
					if (num2 < radius)
					{
						ContextSteeringObstacle item = new ContextSteeringObstacle(vector, errorVec, num2);
						list.Add(item);
					}
				}
			}
		}
		return list.ToArray();
	}
}
