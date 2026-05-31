using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown.GameData;

public struct SpawnPointGroup
{
	public readonly int TotalCapacity;

	public readonly int MaxCapacity;

	public readonly bool IsValid;

	public SpawnPoint[] SpawnPoints { get; private set; }

	public int PointCount
	{
		get
		{
			SpawnPoint[] spawnPoints = SpawnPoints;
			if (spawnPoints == null)
			{
				return 0;
			}
			return spawnPoints.Length;
		}
	}

	public SpawnPointGroup(Vector2PositionList[] points)
	{
		TotalCapacity = 0;
		MaxCapacity = 0;
		if (points == null || points.IsNullOrEmpty())
		{
			Debug.LogWarning("生成组为空！");
			SpawnPoints = null;
			IsValid = false;
			return;
		}
		IsValid = true;
		SpawnPoints = new SpawnPoint[points.Length];
		for (int i = 0; i < points.Length; i++)
		{
			SpawnPoints[i] = new SpawnPoint(i, points[i].positions);
			IsValid &= SpawnPoints[i].IsValid;
			TotalCapacity += SpawnPoints[i].MaxCapacity;
			MaxCapacity = Mathf.Max(MaxCapacity, SpawnPoints[i].MaxCapacity);
		}
	}

	public Vector2 GetRandomPosition()
	{
		if (!IsValid)
		{
			return Vector2.zero;
		}
		return SpawnPoints[Random.Range(0, PointCount)].GetRandomPosition();
	}

	public void ClearBuffer()
	{
		for (int i = 0; i < PointCount; i++)
		{
			SpawnPoints[i].ClearBuffer();
		}
	}

	public bool AssignToSpawnGroup(ISpawnedItem[] spawnedItems, out Dictionary<string, List<Vector2>> spawnedPositions)
	{
		ClearBuffer();
		bool flag = AssignRecursively(spawnedItems, 0);
		spawnedPositions = new Dictionary<string, List<Vector2>>();
		if (!flag)
		{
			ClearBuffer();
			ISpawnedItem[] array = spawnedItems.OrderByDescending((ISpawnedItem x) => x.Volume).ToArray();
			int[] array2 = (from x in SpawnPoints
				orderby x.MaxCapacity descending
				select x.Index).ToArray();
			ISpawnedItem[] array3 = array;
			foreach (ISpawnedItem item in array3)
			{
				for (int j = 0; j < PointCount; j++)
				{
					int num = array2[j];
					if (SpawnPoints[num].CanAddSpawnItem(item))
					{
						SpawnPoints[num].AddSpawnedItem(item);
						break;
					}
				}
			}
		}
		SpawnPoint[] spawnPoints = SpawnPoints;
		foreach (SpawnPoint spawnPoint in spawnPoints)
		{
			spawnPoint.GetBufferItemSpawnPositions(ref spawnedPositions);
		}
		ClearBuffer();
		return flag;
	}

	private bool AssignRecursively(ISpawnedItem[] spawnedItems, int index)
	{
		if (index >= spawnedItems.Length)
		{
			return true;
		}
		ISpawnedItem item = spawnedItems[index];
		int totalCapacity = TotalCapacity;
		SpawnPoint[] array = SpawnPoints.ShuffleByWeight((SpawnPoint x) => (float)(x.MaxCapacity - x.UsedCapacity) / (float)totalCapacity + Random.value / 2f);
		for (int i = 0; i < array.Length; i++)
		{
			int index2 = array[i].Index;
			if (SpawnPoints[index2].CanAddSpawnItem(item))
			{
				SpawnPoints[index2].AddSpawnedItem(item);
				if (AssignRecursively(spawnedItems, index + 1))
				{
					return true;
				}
				SpawnPoints[index2].RemoveSpawnedItem(item);
			}
		}
		return false;
	}
}
