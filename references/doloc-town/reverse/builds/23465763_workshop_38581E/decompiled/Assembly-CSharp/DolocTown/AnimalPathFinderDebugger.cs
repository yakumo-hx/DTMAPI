using System;
using DolocTown.Editor;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AnimalPathFinderDebugger : MonoBehaviour
{
	[SerializeField]
	private int animalWidth = 1;

	[SerializeField]
	private bool shouldOptimize;

	[SerializeField]
	private PositionMarker start;

	[SerializeField]
	private PositionMarker end;

	[SerializeField]
	private bool drawAnimalMapForBuilding;

	private AnimalPathFinder pathFinder;

	private AnimalPathFinderStair _pathFinderStair;

	private AnimalPathFinderWidth _pathFinderWidth;

	private AnimalMap animalMap;

	private Vector2Int[] currentPath;

	private Vector2Int[] currentAvailablePositions;

	private AnimalStair[] currentAvailableStairs;

	public AnimalPathFinder PathFinder
	{
		get
		{
			if (pathFinder == null || pathFinder.room != DolocAPI.CurrentRoom)
			{
				pathFinder = new AnimalPathFinder(DolocAPI.CurrentRoom);
			}
			return pathFinder;
		}
	}

	public AnimalPathFinderStair PathFinderStair
	{
		get
		{
			if (animalMap == null)
			{
				return null;
			}
			if (_pathFinderStair == null || _pathFinderStair.animalWidth != animalWidth)
			{
				_pathFinderStair = new AnimalPathFinderStair(animalMap, animalWidth);
			}
			return _pathFinderStair;
		}
	}

	public AnimalPathFinderWidth PathFinderWidth
	{
		get
		{
			if (_pathFinderWidth == null || _pathFinderWidth.width != animalWidth || _pathFinderWidth.room != DolocAPI.CurrentRoom)
			{
				_pathFinderWidth = new AnimalPathFinderWidth(DolocAPI.CurrentRoom, animalWidth);
			}
			return _pathFinderWidth;
		}
	}

	public void InitializeRoomEnvironment()
	{
		animalMap = new AnimalMap(DolocAPI.CurrentRoom);
	}

	public void FindAvailablePositions()
	{
		Vector2Int gridPosition = start.GridPosition;
		currentAvailablePositions = AnimalUtils.FindAvailablePositions(DolocAPI.CurrentRoom, gridPosition, animalWidth);
	}

	private void SerachNearStairs(int touchThreshold = 0)
	{
		if (!animalMap.TryFindStair(start.GridPosition, out var stair))
		{
			Debug.LogError("未找到临近台阶");
			return;
		}
		Debug.Log($"<color=red>当前所在台阶为:{stair.start},{stair.length}</color>");
		currentAvailableStairs = animalMap.TryGetNearStairs(stair);
		if (currentAvailableStairs == null || currentAvailableStairs.Length == 0)
		{
			Debug.LogError("未找到临近台阶");
		}
	}

	public void ClearAvailablePositions()
	{
		currentAvailablePositions = Array.Empty<Vector2Int>();
	}

	public void FindPath()
	{
		AnimalPathFinder animalPathFinder = PathFinder;
		if (animalPathFinder == null)
		{
			Debug.LogError("PathFinder is not initialized.");
			return;
		}
		Vector2Int gridPosition = start.GridPosition;
		Vector2Int gridPosition2 = end.GridPosition;
		currentPath = animalPathFinder.FindPath(gridPosition, gridPosition2);
		if (currentPath == null)
		{
			Debug.LogError($"寻路失败:{gridPosition},{gridPosition2}");
		}
	}

	public void FindPathWidth()
	{
		AnimalPathFinderWidth pathFinderWidth = PathFinderWidth;
		if (pathFinderWidth == null)
		{
			Debug.LogError("PathFinder is not initialized.");
			return;
		}
		Vector2Int gridPosition = start.GridPosition;
		Vector2Int gridPosition2 = end.GridPosition;
		currentPath = pathFinderWidth.FindPath(gridPosition, gridPosition2);
		if (currentPath == null)
		{
			Debug.LogError($"寻路失败:{gridPosition},{gridPosition2}");
		}
		else if (shouldOptimize)
		{
			currentPath = AnimalPathFinderUtils.OptimizePath(currentPath);
		}
	}

	public void FindPathStair()
	{
		AnimalPathFinderStair pathFinderStair = PathFinderStair;
		if (pathFinderStair == null)
		{
			Debug.LogError("PathFinderStair is not initialized.");
			return;
		}
		Vector2Int gridPosition = start.GridPosition;
		Vector2Int gridPosition2 = end.GridPosition;
		if (!animalMap.TryFindStair(gridPosition, out var stair) || !animalMap.TryFindStair(gridPosition2, out var stair2))
		{
			Debug.LogError($"未找到台阶: {gridPosition} 或 {gridPosition2}");
		}
		else
		{
			currentPath = pathFinderStair.FindPathStair(stair.start, stair2.start);
		}
	}

	public void IsWalkable()
	{
		Vector2Int gridPosition = start.GridPosition;
		Debug.Log($"{gridPosition}可行走性\"{AnimalUtils.IsPositionWalkable(DolocAPI.CurrentRoom, gridPosition, animalWidth)}\"");
	}

	private void RefreshAnimalMap()
	{
		AnimalUtils.AnimalMapForBuilding.Refresh();
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying && DolocAPI.dataPersistenceManager != null)
		{
			DrawPath();
			DrawAvailablePositions();
			DrawAnimalMap();
			DrawNearStairs();
			DrawAnimalMapForBuilding();
		}
	}

	private void DrawAnimalMapForBuilding()
	{
		if (!drawAnimalMapForBuilding)
		{
			return;
		}
		foreach (Vector2Int allPosition in AnimalUtils.AnimalMapForBuilding.AllPositions)
		{
			GizmosHelper.DrawBoxMM(DolocAPI.CurrentRoom.Geometry.CalcWorldPositionCenter(allPosition), DolocTransform.TILE_WORLD_SIZE, Color.yellow);
		}
	}

	private void DrawAvailablePositions()
	{
		if (!currentAvailablePositions.IsNullOrEmpty())
		{
			Gizmos.color = Color.yellow;
			Vector2Int[] array = currentAvailablePositions;
			foreach (Vector2Int cellPosition in array)
			{
				GizmosHelper.DrawBoxMM(DolocAPI.CurrentRoom.Geometry.CalcWorldPositionCenter(cellPosition), DolocTransform.TILE_WORLD_SIZE, Color.yellow);
			}
		}
	}

	private void DrawPath()
	{
		if (DolocAPI.CurrentRoom == null || currentPath == null)
		{
			return;
		}
		Gizmos.color = Color.green;
		Vector2Int vector2Int = currentPath[0];
		Vector2 vector = DolocAPI.CurrentRoom.Geometry.CalcWorldPositionCenter(vector2Int);
		Vector2Int[] array = currentPath;
		foreach (Vector2Int vector2Int2 in array)
		{
			Vector2 vector2 = DolocAPI.CurrentRoom.Geometry.CalcWorldPositionCenter(vector2Int2);
			GizmosHelper.DrawBoxMM(vector2, DolocTransform.TILE_WORLD_SIZE);
			if (vector2Int2 != vector2Int)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(vector, vector2);
				Gizmos.color = Color.green;
				vector2Int = vector2Int2;
				vector = vector2;
			}
		}
		Gizmos.color = Color.white;
	}

	private void DrawAnimalMap(float padding = 0.1f)
	{
		if (animalMap == null)
		{
			return;
		}
		Gizmos.color = Color.cyan;
		Room currentRoom = DolocAPI.CurrentRoom;
		Vector2 vector = new Vector2(padding, padding);
		Vector2 vector2 = vector * 2f;
		foreach (RectInt allArea in animalMap.AllAreas)
		{
			Vector2 vector3 = currentRoom.Geometry.CalcWorldPosition(allArea.position);
			GizmosHelper.DrawRect(new Rect(vector3 + vector, allArea.size * DolocTransform.TILE_WORLD_SIZE - vector2));
		}
	}

	private void DrawNearStairs(float padding = 0.1f)
	{
		if (currentAvailableStairs != null && currentAvailableStairs.Length != 0)
		{
			Gizmos.color = Color.red;
			Vector2 vector = new Vector2(padding, padding);
			Vector2 vector2 = vector * 2f;
			AnimalStair[] array = currentAvailableStairs;
			for (int i = 0; i < array.Length; i++)
			{
				AnimalStair animalStair = array[i];
				RectInt area = animalStair.Area;
				Vector2 vector3 = DolocAPI.CurrentRoom.Geometry.CalcWorldPosition(area.position);
				GizmosHelper.DrawRect(new Rect(vector3 + vector, new Vector2(animalStair.length, 1f) * DolocTransform.TILE_WORLD_SIZE - vector2));
			}
		}
	}
}
