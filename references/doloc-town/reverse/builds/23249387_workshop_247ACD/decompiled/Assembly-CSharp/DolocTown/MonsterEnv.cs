using System;
using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw.AI;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public class MonsterEnv
{
	private readonly IMonsterHost monsterHost;

	private readonly ObstacleFinder _obstacleFinder;

	private readonly MonsterGroupManager GroupManager = new MonsterGroupManager();

	public readonly IGameMap GroundMap;

	public readonly IGameMap AirMap;

	public IEnumerable<Monster> AllMonsters => monsterHost.DM_monster.AllMonsters;

	public int Count => monsterHost.MonsterCount;

	public Vector2 RandomGroundPosition
	{
		get
		{
			Vector2 pivot = new Vector2(0.5f, 0f);
			return GroundMap.GetRandomGroundPositionWS(pivot);
		}
	}

	public MonsterEnv(Room room)
	{
		monsterHost = room ?? throw new ArgumentNullException("room");
		(IGameMap, IGameMap) tuple = room.baseProto.CreateGameMaps(DolocTransform.TILE_WORLD_SIZE);
		AirMap = tuple.Item1;
		GroundMap = tuple.Item2;
		_obstacleFinder = new ObstacleFinder(AirMap);
	}

	public void InvokeEnv()
	{
		GroupManager.Reset(monsterHost);
	}

	public void DisposeEnv()
	{
		GroupManager.Dispose();
	}

	public IGameMap GetMap(bool isAir)
	{
		if (!isAir)
		{
			return GroundMap;
		}
		return AirMap;
	}

	public void TryAddMonster(MonsterController controller)
	{
		GroupManager.TryAddMonster(controller);
	}

	public Vector2[] CellToWorldCenter(Vector2Int[] pos)
	{
		return AirMap.CellToWorldCenter(pos);
	}

	public ContextSteeringObstacle[] GetObstacles(Vector3 position, float radius)
	{
		return _obstacleFinder.GetObstacles(position, radius);
	}

	public bool CallMonster(string monsterId, out Monster monster)
	{
		if (!DolocAPI.assets.monsters.QueryMonster(monsterId, out var proto))
		{
			Debug.LogError("未找到指定的怪物ID:\"" + monsterId + "\"");
			monster = null;
			return false;
		}
		monster = monsterHost.GenerateMonster(proto);
		return monster != null;
	}

	public bool CallMonsters(string monsterId, int count = 1)
	{
		if (!DolocAPI.assets.monsters.QueryMonster(monsterId, out var proto))
		{
			Debug.LogError("未找到指定的怪物ID:\"" + monsterId + "\"");
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			monsterHost.GenerateMonster(proto);
		}
		return true;
	}
}
