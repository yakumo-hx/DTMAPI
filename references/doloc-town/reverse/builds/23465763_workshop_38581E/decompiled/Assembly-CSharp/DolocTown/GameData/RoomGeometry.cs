using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown.GameData;

public readonly struct RoomGeometry
{
	public readonly Vector2 scenePosition;

	public readonly Vector2 sceneSize;

	public readonly Vector4 cameraPadding;

	public readonly Vector2 roomPosition;

	public readonly Vector2 roomSize;

	public readonly Vector2Int gridPos;

	public readonly Vector2Int gridSize;

	private readonly Vector2 defaultEntryPosition;

	public readonly Vector2Int[] obstacles;

	public readonly Vector2Int[] groundPositions;

	public readonly Vector2Int[] extraObstacles;

	public readonly int[,] mapInfo;

	private readonly Dictionary<int, int[]> groundHeights;

	public readonly Vector2Int[] totalObstacles;

	public readonly Vector2 RoomPositionRT;

	public readonly Vector2[] polygon;

	public Rect SceneRect => new Rect(scenePosition, sceneSize);

	public Vector2 DefaultEntryPosition => defaultEntryPosition + roomPosition;

	public Vector2Int RandomGridPos
	{
		get
		{
			if (gridSize.x == 0 || gridSize.y == 0)
			{
				return Vector2Int.zero;
			}
			return new Vector2Int(UnityEngine.Random.Range(0, gridSize.x), UnityEngine.Random.Range(0, gridSize.y));
		}
	}

	public RoomGeometry(Vector2 scenePosition, Vector2 sceneSize, Vector4 cameraPadding, Vector2 roomPosition, Vector2 roomSize, Vector2Int gridPos, Vector2Int gridSize, Vector2 defaultEntryPosition, Vector2Int[] obstacles, Vector2Int[] groundPositions, Vector2Int[] extraObstacles, Vector2[] polygon = null)
	{
		this.scenePosition = scenePosition;
		this.sceneSize = sceneSize;
		this.cameraPadding = cameraPadding;
		this.roomPosition = roomPosition;
		this.roomSize = roomSize;
		this.gridPos = gridPos;
		this.gridSize = gridSize;
		this.defaultEntryPosition = defaultEntryPosition;
		this.obstacles = obstacles;
		this.extraObstacles = extraObstacles ?? Array.Empty<Vector2Int>();
		this.groundPositions = groundPositions;
		mapInfo = new int[gridSize.x, gridSize.y];
		Vector2Int[] array = groundPositions;
		for (int i = 0; i < array.Length; i++)
		{
			Vector2Int vector2Int = array[i];
			mapInfo[vector2Int.x, vector2Int.y] = 2;
		}
		array = obstacles;
		for (int i = 0; i < array.Length; i++)
		{
			Vector2Int vector2Int2 = array[i];
			mapInfo[vector2Int2.x, vector2Int2.y] = 1;
		}
		groundHeights = GenHeightMap(groundPositions);
		totalObstacles = new Vector2Int[obstacles.Length + this.extraObstacles.Length];
		obstacles.CopyTo(totalObstacles, 0);
		this.extraObstacles.CopyTo(totalObstacles, obstacles.Length);
		RoomPositionRT = roomPosition + roomSize;
		this.polygon = polygon ?? new Vector2[4]
		{
			roomPosition,
			new Vector2(roomPosition.x + roomSize.x, roomPosition.y),
			new Vector2(roomPosition.x + roomSize.x, roomPosition.y + roomSize.y),
			new Vector2(roomPosition.x, roomPosition.y + roomSize.y)
		};
	}

	public bool Contains(Vector2 position)
	{
		if (position.x > roomPosition.x && position.x < RoomPositionRT.x && position.y > roomPosition.y)
		{
			return position.y < RoomPositionRT.y;
		}
		return false;
	}

	public bool ExtendContains(Vector2 position, float extendPadding)
	{
		Vector2 vector = roomPosition - new Vector2(extendPadding, extendPadding);
		Vector2 vector2 = RoomPositionRT + new Vector2(extendPadding, extendPadding);
		if (position.x > vector.x && position.x < vector2.x && position.y > vector.y)
		{
			return position.y < vector2.y;
		}
		return false;
	}

	public Vector2 Constarint(Vector2 position, float padding)
	{
		return new Vector2(Mathf.Clamp(position.x, roomPosition.x + padding, RoomPositionRT.x - padding), Mathf.Clamp(position.y, roomPosition.y + padding, RoomPositionRT.y - padding));
	}

	public bool Contains(Vector2Int pos)
	{
		if (pos.x >= 0 && pos.x < gridSize.x && pos.y >= 0)
		{
			return pos.y < gridSize.y;
		}
		return false;
	}

	public Vector2 GetRandomSurfacePosition()
	{
		if (groundPositions.IsNullOrEmpty())
		{
			return Vector2.zero;
		}
		Vector2Int cellPosition = groundPositions[UnityEngine.Random.Range(0, groundPositions.Length)];
		return CalcWorldPosition(cellPosition, new Vector2(0.5f, 0f));
	}

	public Vector2 GetRandomSurfacePosition(Vector2 pivot)
	{
		if (groundPositions.IsNullOrEmpty())
		{
			return Vector2.zero;
		}
		Vector2Int cellPosition = groundPositions[UnityEngine.Random.Range(0, groundPositions.Length)];
		return CalcWorldPosition(cellPosition);
	}

	public Vector2 Constraint(Vector2 pos, Vector2 padding)
	{
		return new Vector2(Mathf.Clamp(pos.x, roomPosition.x + padding.x, roomPosition.x + roomSize.x - padding.x), Mathf.Clamp(pos.y, roomPosition.y + padding.y, roomPosition.y + roomSize.y - padding.y));
	}

	public Vector2Int GetNearestEmptyPosition(Vector2Int pos)
	{
		if (!Contains(pos) || mapInfo[pos.x, pos.y] == 0)
		{
			return pos;
		}
		int num = 1;
		for (int i = 0; i < 5; i++)
		{
			Vector2Int[] array = Ring(pos, num + i);
			for (int j = 0; j < array.Length; j++)
			{
				Vector2Int vector2Int = array[j];
				if (Contains(vector2Int) && mapInfo[vector2Int.x, vector2Int.y] == 0)
				{
					return vector2Int;
				}
			}
		}
		return pos;
	}

	public Vector2Int GetNearestGroundPosition(Vector2Int position)
	{
		if (groundPositions.Length == 0)
		{
			return position;
		}
		Vector2Int[] array = new Vector2Int[groundPositions.Length];
		Array.Copy(groundPositions, array, groundPositions.Length);
		Vector2Int[] array2 = array.OrderBy((Vector2Int x) => Mathf.Abs(x.x - position.x) + Mathf.Abs(x.y - position.y)).ToArray();
		if (array2.Length == 0)
		{
			return position;
		}
		return array2[0];
	}

	public bool TryGetNearestGroundPosition(Vector2Int pos, out Vector2Int targetPosition)
	{
		targetPosition = pos;
		if (IsOnGround(pos))
		{
			return true;
		}
		int num = 1;
		for (int i = 0; i < 5; i++)
		{
			Vector2Int[] array = Ring(pos, num + i);
			foreach (Vector2Int vector2Int in array)
			{
				if (IsOnGround(vector2Int))
				{
					targetPosition = vector2Int;
					return true;
				}
			}
		}
		return false;
	}

	public bool IsValidPosition(Vector2 worldPos)
	{
		if (worldPos.x >= roomPosition.x && worldPos.x < roomPosition.x + roomSize.x && worldPos.y >= roomPosition.y)
		{
			return worldPos.y < roomPosition.y + roomSize.y;
		}
		return false;
	}

	public bool IsValidPositionX(float worldPosX, float padding = 0f)
	{
		if (worldPosX >= roomPosition.x + padding)
		{
			return worldPosX < roomPosition.x + roomSize.x - padding;
		}
		return false;
	}

	public bool IsValidPositionY(float worldPosY)
	{
		if (worldPosY >= roomPosition.y)
		{
			return worldPosY < roomPosition.y + roomSize.y;
		}
		return false;
	}

	public Vector2 CalPosPercent(Vector2 worldPos)
	{
		float x = Mathf.Clamp((worldPos.x - roomPosition.x) / roomSize.x, 0f, 1f);
		float y = Mathf.Clamp((worldPos.y - roomPosition.y) / roomSize.y, 0f, 1f);
		return new Vector2(x, y);
	}

	public Vector2Int[] Ring(Vector2Int center, int dst)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		int num = center.y - dst;
		int num2 = center.y + dst;
		int num3 = center.x - dst;
		int num4 = center.x + dst;
		for (int num5 = num2; num5 >= num; num5--)
		{
			list.Add(new Vector2Int(num3, num5));
			list.Add(new Vector2Int(num4, num5));
		}
		num3++;
		for (int i = num3; i < num4; i++)
		{
			list.Add(new Vector2Int(i, num2));
			list.Add(new Vector2Int(i, num));
		}
		return list.ToArray();
	}

	public bool CanPlaceAgent(Vector2Int pos)
	{
		if (IsOnGround(pos) && IsOnGround(new Vector2Int(pos.x + 1, pos.y)) && IsEmptyCell(new Vector2Int(pos.x, pos.y + 1)))
		{
			return IsEmptyCell(new Vector2Int(pos.x + 1, pos.y + 1));
		}
		return false;
	}

	public bool GetNearestGroundPositionCanPlaceAgent(Vector2Int pos, out Vector2Int groundPosition)
	{
		Vector2Int[] array = groundPositions.OrderBy((Vector2Int x) => Mathf.Abs(x.x - pos.x) + Mathf.Abs(x.y - pos.y)).ToArray();
		foreach (Vector2Int vector2Int in array)
		{
			if (CanPlaceAgent(vector2Int))
			{
				groundPosition = vector2Int;
				return true;
			}
		}
		groundPosition = Vector2Int.zero;
		return false;
	}

	public bool RaycastGround(Vector2Int position, out Vector2Int groundPosition)
	{
		groundPosition = position;
		while (groundPosition.y >= 0)
		{
			if (IsOnGround(groundPosition))
			{
				return true;
			}
			groundPosition.y--;
		}
		return false;
	}

	public bool IsEmptyCell(Vector2Int cellPos)
	{
		if (Contains(cellPos))
		{
			return mapInfo[cellPos.x, cellPos.y] == 0;
		}
		return false;
	}

	public bool IsObstacle(Vector2Int cellPos)
	{
		if (Contains(cellPos))
		{
			return mapInfo[cellPos.x, cellPos.y] == 1;
		}
		return false;
	}

	public bool IsNotObstacle(Vector2Int cellpos)
	{
		if (Contains(cellpos))
		{
			return mapInfo[cellpos.x, cellpos.y] != 1;
		}
		return false;
	}

	public bool IsOnGround(Vector2Int pos)
	{
		if (groundHeights.TryGetValue(pos.y, out var value))
		{
			return value.Contains(pos.x);
		}
		return false;
	}

	public Vector2[] GetObstaclesWS(Vector2 cellsize)
	{
		return obstacles.Select((Vector2Int x) => x * cellsize).ToArray();
	}

	public Vector2Int CalcCellPosition(Vector2 positionWS)
	{
		Vector2 vector = positionWS - roomPosition;
		return new Vector2Int(Mathf.RoundToInt(vector.x * 0.66667f), Mathf.RoundToInt(vector.y * 0.66667f));
	}

	public Vector2Int CalcMinCellPosition(Vector2 positionWS)
	{
		Vector2 vector = positionWS - roomPosition;
		return new Vector2Int((int)(vector.x * 0.66667f), (int)(vector.y * 0.66667f));
	}

	public Vector2Int CalcFaceCellPosition(Vector2 positionWS, bool resetPos = true)
	{
		Vector2 vector = positionWS;
		if (resetPos)
		{
			vector -= roomPosition;
		}
		return new Vector2Int((int)(vector.x * 0.66667f), (int)(vector.y * 0.66667f)) + new Vector2Int(DolocAPI.AgentFaceRight ? 1 : 0, 0);
	}

	public Vector2 CalcWorldPosition(Vector2Int cellPosition)
	{
		return (Vector2)cellPosition * 1.5f + roomPosition;
	}

	public Vector2 CalcWorldPosition(Vector2Int cellPosition, Vector2 pivot)
	{
		return (cellPosition + pivot) * 1.5f + roomPosition;
	}

	public Vector2 CalcWorldPositionCenter(Vector2Int cellPosition)
	{
		return (Vector2)cellPosition * 1.5f + roomPosition + DolocTransform.TILE_WORLD_SIZE * 0.5f;
	}

	public Vector2 CalcWorldPosition(Vector2Int cellpos, Vector2Int size)
	{
		return roomPosition + new Vector2((float)cellpos.x + (float)size.x * 0.5f, cellpos.y) * 1.5f;
	}

	private static Dictionary<int, int[]> GenHeightMap(Vector2Int[] groundPositions)
	{
		if (groundPositions.IsNullOrEmpty())
		{
			return new Dictionary<int, int[]>();
		}
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		for (int i = 0; i < groundPositions.Length; i++)
		{
			Vector2Int vector2Int = groundPositions[i];
			if (!dictionary.ContainsKey(vector2Int.y))
			{
				dictionary.Add(vector2Int.y, new List<int>());
			}
			dictionary[vector2Int.y].Add(vector2Int.x);
		}
		return dictionary.ToDictionary((KeyValuePair<int, List<int>> x) => x.Key, (KeyValuePair<int, List<int>> x) => x.Value.ToArray());
	}
}
