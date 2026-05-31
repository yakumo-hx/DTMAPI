using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using RedSaw;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public static class RoomHandleUtils
{
	public static Vector4 CalculateRoomGeometry(GameObject targetObject)
	{
		if (targetObject == null)
		{
			return Vector4.zero;
		}
		Tilemap componentInChildren = targetObject.GetComponentInChildren<Tilemap>();
		if (componentInChildren != null)
		{
			Vector2 vector = componentInChildren.GetComponentInParent<Grid>().cellSize;
			Vector2 vector2 = componentInChildren.transform.position;
			Vector2 vector3 = new Vector2((float)componentInChildren.size.x * vector.x, (float)componentInChildren.size.y * vector.y);
			return new Vector4(vector2.x, vector2.y, vector3.x, Mathf.Max(33.75f, vector3.y));
		}
		EdgeCollider2D componentInChildren2 = targetObject.GetComponentInChildren<EdgeCollider2D>();
		if (componentInChildren2 != null)
		{
			Vector2 vector4 = componentInChildren2.points[0];
			Vector2 vector5 = vector4;
			Vector2[] points = componentInChildren2.points;
			for (int i = 0; i < points.Length; i++)
			{
				Vector2 vector6 = points[i];
				vector4.x = Mathf.Min(vector6.x, vector4.x);
				vector4.y = Mathf.Min(vector6.y, vector4.y);
				vector5.x = Mathf.Max(vector6.x, vector5.x);
				vector5.y = Mathf.Max(vector6.y, vector5.y);
			}
			Vector2 vector7 = vector5 - vector4;
			Vector2 vector8 = componentInChildren2.transform.position;
			return new Vector4(vector8.x - vector7.x / 2f, vector8.y, vector7.x, vector7.y);
		}
		return Vector4.zero;
	}

	public static Vector2Int[] GetObstacles(Tilemap tilemap, Tilemap extraTilemap, Vector2 scenePosition, Vector2 sceneSize)
	{
		if (tilemap == null)
		{
			return Array.Empty<Vector2Int>();
		}
		Vector2Int pos = scenePosition.SnapToGrid(tilemap.cellSize);
		Vector2Int size = sceneSize.SnapToGrid(tilemap.cellSize);
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(tilemap.GetObstacles(pos, size));
		if (extraTilemap != null)
		{
			hashSet.AddRange(extraTilemap.GetObstacles(pos, size));
		}
		return hashSet.ToArray();
	}

	public static Vector2Int[] GetObstacles(Tilemap tilemap, Tilemap extraTilemap)
	{
		if (tilemap == null)
		{
			return Array.Empty<Vector2Int>();
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(tilemap.GetObstacles());
		if (extraTilemap != null)
		{
			hashSet.AddRange(extraTilemap.GetObstacles());
		}
		return hashSet.ToArray();
	}

	public static Vector2Int[] GetExtraObstacles(Tilemap tilemap)
	{
		if (tilemap == null)
		{
			return Array.Empty<Vector2Int>();
		}
		return new HashSet<Vector2Int>(tilemap.GetObstacles()).ToArray();
	}

	public static bool CalcGroundHeightFromTilemap(RoomGeometrySO geometry, out float groundHeight)
	{
		Vector2Int[] groundPositions = geometry.groundPositions;
		if (groundPositions.IsNullOrEmpty())
		{
			groundHeight = 0f;
			return true;
		}
		int maxFrequencyHeight = GetMaxFrequencyHeight(groundPositions);
		groundHeight = (float)maxFrequencyHeight * 1.5f - 0.125f;
		return true;
	}

	private static int GetMaxFrequencyHeight(Vector2Int[] positions)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		for (int i = 0; i < positions.Length; i++)
		{
			Vector2Int vector2Int = positions[i];
			if (!dictionary.TryAdd(vector2Int.y, 1))
			{
				dictionary[vector2Int.y]++;
			}
		}
		int num = 0;
		int result = 0;
		foreach (KeyValuePair<int, int> item in dictionary)
		{
			if (item.Value > num)
			{
				num = item.Value;
				result = item.Key;
			}
		}
		return result;
	}

	public static void SnapEdgeColliderToRect(EdgeCollider2D collider, Vector2 pos, Vector2 size, Vector2 padding)
	{
		if (!(collider == null))
		{
			Vector2[] array = new Vector2[5];
			Vector2 vector = pos - (Vector2)collider.transform.position + padding;
			Vector2 vector2 = size - padding * 2f;
			array[0] = vector;
			array[1] = vector + new Vector2(vector2.x, 0f);
			array[2] = vector + vector2;
			array[3] = vector + new Vector2(0f, vector2.y);
			array[4] = vector;
			collider.points = array;
		}
	}
}
