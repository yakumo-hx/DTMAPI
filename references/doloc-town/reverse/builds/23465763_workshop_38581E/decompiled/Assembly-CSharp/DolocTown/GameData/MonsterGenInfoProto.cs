using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown.GameData;

public struct MonsterGenInfoProto
{
	public readonly bool shouldGen;

	public static MonsterGenInfoProto Empty => new MonsterGenInfoProto(null, null, shouldGen: false);

	public SpawnPointGroup airSpawnGroup { get; private set; }

	public SpawnPointGroup gdSpawnGroup { get; private set; }

	public MonsterGenInfoProto(Vector2PositionList[] air, Vector2PositionList[] gd, bool shouldGen)
	{
		this.shouldGen = shouldGen;
		airSpawnGroup = ((shouldGen && air != null && air.Length > 0) ? new SpawnPointGroup(air) : default(SpawnPointGroup));
		gdSpawnGroup = ((shouldGen && gd != null && gd.Length > 0) ? new SpawnPointGroup(gd) : default(SpawnPointGroup));
	}

	public bool GetRandomMonsterPoint(bool isAir, out Vector2 pos)
	{
		return GetRandomPoint(isAir ? airSpawnGroup : gdSpawnGroup, out pos);
	}

	private bool GetRandomPoint(SpawnPointGroup group, out Vector2 pos)
	{
		pos = group.GetRandomPosition();
		return group.IsValid;
	}

	public Dictionary<string, List<Vector2>> AssignMonstersToSpawnGroups((MonsterProto, int)[] monstersWithCount, string debugInfo = "")
	{
		List<ISpawnedItem> list = new List<ISpawnedItem>();
		List<ISpawnedItem> list2 = new List<ISpawnedItem>();
		for (int i = 0; i < monstersWithCount.Length; i++)
		{
			MonsterProto item = monstersWithCount[i].Item1;
			int item2 = monstersWithCount[i].Item2;
			bool isAir = item.IsAir;
			for (int j = 0; j < item2; j++)
			{
				if (isAir)
				{
					list.Add(item);
				}
				else
				{
					list2.Add(item);
				}
			}
		}
		Dictionary<string, List<Vector2>> spawnPositions = new Dictionary<string, List<Vector2>>();
		AssignMonsters(list.ToArray(), airSpawnGroup, ref spawnPositions, debugInfo + "[空中]");
		AssignMonsters(list2.ToArray(), gdSpawnGroup, ref spawnPositions, debugInfo + "[地面]");
		return spawnPositions;
	}

	private void AssignMonsters(ISpawnedItem[] spawnedItems, SpawnPointGroup spawnPointGroup, ref Dictionary<string, List<Vector2>> spawnPositions, string debugInfo = "")
	{
		if (spawnPointGroup.IsValid)
		{
			if (!spawnPointGroup.AssignToSpawnGroup(spawnedItems.Shuffle(), out var spawnedPositions))
			{
				Debug.LogWarning(debugInfo + ": 未能完全放置怪物！");
			}
			{
				foreach (KeyValuePair<string, List<Vector2>> item in spawnedPositions)
				{
					spawnPositions[item.Key] = item.Value;
				}
				return;
			}
		}
		Debug.LogWarning(debugInfo + ": 未能完全放置怪物！");
	}
}
