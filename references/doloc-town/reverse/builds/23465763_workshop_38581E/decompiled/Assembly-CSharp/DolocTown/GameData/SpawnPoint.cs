using System.Collections.Generic;
using System.Linq;
using ParadoxNotion;
using UnityEngine;

namespace DolocTown.GameData;

public struct SpawnPoint
{
	public readonly int Index;

	public readonly Vector2[] Positions;

	public readonly bool IsValid;

	private List<ISpawnedItem> bufferedItems;

	public int MaxCapacity
	{
		get
		{
			Vector2[] positions = Positions;
			if (positions == null)
			{
				return 0;
			}
			return positions.Length;
		}
	}

	public int UsedCapacity { get; private set; }

	public SpawnPoint(int index, Vector2[] positions)
	{
		Index = index;
		bufferedItems = new List<ISpawnedItem>();
		UsedCapacity = 0;
		if (positions == null || positions.IsNullOrEmpty())
		{
			Debug.LogWarning("生成点为空！");
			Positions = new Vector2[1] { Vector2.zero };
			IsValid = false;
		}
		else
		{
			IsValid = true;
			Positions = positions;
		}
	}

	public Vector2 GetRandomPosition()
	{
		if (!IsValid)
		{
			return Vector2.zero;
		}
		return Positions[Random.Range(0, MaxCapacity)];
	}

	public void GetBufferItemSpawnPositions(ref Dictionary<string, List<Vector2>> spawnPoints)
	{
		if (bufferedItems.IsNullOrEmpty())
		{
			return;
		}
		List<ISpawnedItem> list = bufferedItems.ToList();
		int usedCapacity = UsedCapacity;
		int num = MaxCapacity - UsedCapacity;
		for (int i = 0; i < num; i++)
		{
			bufferedItems.Add(new SpawnedItemHoldPlace(1));
		}
		if (num != 0)
		{
			bufferedItems.Shuffle();
		}
		int num2 = 0;
		foreach (ISpawnedItem bufferedItem in bufferedItems)
		{
			if (!string.IsNullOrEmpty(bufferedItem.SpawnId))
			{
				spawnPoints.TryAdd(bufferedItem.SpawnId, new List<Vector2>());
				spawnPoints[bufferedItem.SpawnId].Add(Positions[num2]);
			}
			num2 += bufferedItem.Volume;
		}
		bufferedItems = list;
		UsedCapacity = usedCapacity;
	}

	public bool CanAddSpawnItem(ISpawnedItem item)
	{
		return UsedCapacity + item.Volume <= MaxCapacity;
	}

	public void AddSpawnedItem(ISpawnedItem item)
	{
		bufferedItems.Add(item);
		UsedCapacity += item.Volume;
	}

	public void RemoveSpawnedItem(ISpawnedItem item)
	{
		bufferedItems.Remove(item);
		UsedCapacity -= item.Volume;
	}

	public void ClearBuffer()
	{
		UsedCapacity = 0;
		bufferedItems.Clear();
	}
}
