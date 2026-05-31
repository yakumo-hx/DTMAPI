using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Monster;
using DolocTown.GameData;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public interface IMonsterHost : IBaseHost
{
	MonsterEnv MonsterEnv { get; }

	MonsterGenInfoProto MonsterGenInfo { get; }

	MonsterManager DM_monster { get; }

	int MonsterCount => DM_monster.MonsterCount;

	Monster TryGetFirstMonster(Func<Monster, bool> predicate)
	{
		return DM_monster.AllMonsters.FirstOrDefault(predicate);
	}

	void RemoveMonster(Monster monster)
	{
		if (monster != null)
		{
			DM_monster.RemoveMonster(monster);
			DolocAPI.EntitySystem.Recycle(monster.Controller, monster.proto.Name);
		}
	}

	bool _RunMonster(MonsterEnv env, Monster entity)
	{
		if (entity.Controller != null)
		{
			return true;
		}
		return _RunMonster(env, entity, entity.position);
	}

	bool _RunMonster(MonsterEnv env, Monster entity, Vector2 position)
	{
		entity.position = position;
		entity.Host = this;
		MonsterController monsterController = DolocAPI.EntitySystem.Next<MonsterController>(entity.proto.Name);
		if (monsterController == null)
		{
			return false;
		}
		BattleSystem battleSystem = DolocAPI.battleSystem;
		battleSystem.AddApc(monsterController);
		monsterController.position2d = position;
		monsterController.Run(env, entity, battleSystem);
		return true;
	}

	void GenerateMonster(MonsterProto proto, Vector2 position, bool shouldRender = true)
	{
		Monster monster = DM_monster.CreateMonster(proto);
		monster.position = position;
		monster.Host = this;
		if (shouldRender)
		{
			_RunMonster(MonsterEnv, monster);
		}
	}

	Monster GenerateMonster(MonsterProto proto, bool shouldRender = true)
	{
		if (!MonsterGenInfo.GetRandomMonsterPoint(proto.IsAir, out var pos))
		{
			if (MonsterEnv?.AirMap == null || MonsterEnv?.GroundMap == null)
			{
				Debug.LogError("当前IMonsterHost的环境信息没有设置，无法生成怪物");
				return null;
			}
			IGameMap map = MonsterEnv.GetMap(proto.IsAir);
			pos = (proto.IsAir ? map.GetRandomEmptyPositionWS() : map.GetRandomGroundPositionWS());
		}
		Monster monster = DM_monster.CreateMonster(proto);
		monster.position = pos;
		monster.Host = this;
		if (shouldRender)
		{
			_RunMonster(MonsterEnv, monster);
		}
		return monster;
	}

	Monster[] GenerateMonstersData(MonsterSpawnInfo lut, int totalCount)
	{
		if (totalCount <= 0)
		{
			return Array.Empty<Monster>();
		}
		Dictionary<MonsterSpawnData, int> dictionary = lut.Spawn(totalCount, null, CurrentRoom.RoomId);
		List<(MonsterProto, int)> list = new List<(MonsterProto, int)>();
		foreach (KeyValuePair<MonsterSpawnData, int> item in dictionary)
		{
			if (!DolocAPI.assets.monsters.QueryMonster(item.Key.MonsterId, out var proto))
			{
				Debug.LogWarning($"怪物<{item.Key}>不存在");
			}
			else
			{
				list.Add((proto, item.Value));
			}
		}
		List<Monster> list2 = new List<Monster>();
		foreach (KeyValuePair<string, List<Vector2>> item2 in MonsterGenInfo.AssignMonstersToSpawnGroups(list.ToArray(), CurrentRoom.RoomId))
		{
			if (!DolocAPI.assets.monsters.QueryMonster(item2.Key, out var proto2))
			{
				continue;
			}
			foreach (Vector2 item3 in item2.Value)
			{
				Monster monster = DM_monster.CreateMonster(proto2);
				if (monster == null)
				{
					break;
				}
				monster.position = item3;
				monster.Host = this;
				list2.Add(monster);
			}
		}
		return list2.ToArray();
	}

	Monster[] GenerateMonstersData(MonsterSpawnEntry spawnEntry)
	{
		if (spawnEntry.IsEmpty)
		{
			return Array.Empty<Monster>();
		}
		MonsterSpawnInfo spawnLut_Ref = spawnEntry.SpawnLut_Ref;
		return GenerateMonstersData(spawnLut_Ref, spawnEntry.CountRange.RandomCount);
	}

	Monster[] GenerateMonstersData()
	{
		if (!MonsterGenInfo.shouldGen)
		{
			return Array.Empty<Monster>();
		}
		MonsterSpawnEntry monsterSpawnEntry = CurrentRoom.RoomSpawnInfo.MonsterSpawnEntry;
		return GenerateMonstersData(monsterSpawnEntry);
	}

	void RunMonsters()
	{
		if (DM_monster.IsRunning)
		{
			return;
		}
		DM_monster.SetIsRunning(value: true);
		if (DM_monster.HasMonster)
		{
			Debug.Log("<color=#ff00ff>渲染房间\"" + CurrentRoom.RoomId + "\"的怪物</color>");
		}
		MonsterEnv.InvokeEnv();
		foreach (Monster allMonster in DM_monster.AllMonsters)
		{
			_RunMonster(MonsterEnv, allMonster);
		}
	}

	void HideAllMonsters()
	{
		if (!DM_monster.IsRunning)
		{
			return;
		}
		DM_monster.SetIsRunning(value: false);
		MonsterEnv.DisposeEnv();
		foreach (Monster allMonster in DM_monster.AllMonsters)
		{
			DolocAPI.EntitySystem.Recycle(allMonster.Controller, allMonster.proto.Name);
		}
	}

	void ClearMonsters()
	{
		if (DM_monster.HasNoMonster)
		{
			return;
		}
		foreach (Monster allMonster in DM_monster.AllMonsters)
		{
			DolocAPI.EntitySystem.Recycle(allMonster.Controller, allMonster.proto.Name);
		}
		DM_monster.Clear();
	}

	bool TryGetNearestMonsterInRadius(Vector2 dronePosition, float radius, out Monster monster)
	{
		monster = null;
		if (DM_monster.HasNoMonster)
		{
			return false;
		}
		float num = float.MaxValue;
		foreach (Monster item in DM_monster.AllMonsters.Where((Monster c) => c.Controller != null && !c.Controller.ShieldBullet))
		{
			MonsterController controller = item.Controller;
			Vector2 positionAttack = controller.PositionAttack;
			if (!(Physics2D.Linecast(dronePosition, positionAttack, DolocAPI.gameConfig.DroneFireTestMask).collider != controller.Collider))
			{
				float num2 = Vector2.Distance(dronePosition, positionAttack);
				if (!(num2 >= radius) && !(num2 >= num))
				{
					num = num2;
					monster = item;
				}
			}
		}
		return monster != null;
	}

	bool TryGetNearestMonsterInRadius(Vector2 dronePosition, float radius, float ballisticWidth, out Monster monster)
	{
		monster = null;
		if (DM_monster.HasNoMonster)
		{
			return false;
		}
		float num = float.MaxValue;
		foreach (Monster item in DM_monster.AllMonsters.Where((Monster c) => c.Controller != null && !c.Controller.ShieldBullet))
		{
			MonsterController controller = item.Controller;
			Vector2 positionCenter = controller.PositionCenter;
			float num2 = Vector2.Distance(dronePosition, positionCenter);
			if (num2 >= radius || num2 >= num)
			{
				continue;
			}
			float num3 = ballisticWidth / 2f;
			Vector2 normalized = (positionCenter - dronePosition).normalized;
			Vector2 vector = new Vector2(0f - normalized.y, normalized.x);
			Vector2 start = dronePosition + vector * num3;
			Vector2 end = positionCenter + vector * num3;
			if (!(Physics2D.Linecast(start, end, DolocAPI.gameConfig.DroneFireTestMask).collider != controller.Collider))
			{
				Vector2 start2 = dronePosition - vector * ballisticWidth;
				Vector2 end2 = positionCenter - vector * ballisticWidth;
				if (!(Physics2D.Linecast(start2, end2, DolocAPI.gameConfig.DroneFireTestMask).collider != controller.Collider))
				{
					num = num2;
					monster = item;
				}
			}
		}
		return monster != null;
	}

	bool TryGetNearestTargetInRadius(Vector2 dronePosition, float radius, out StaticTarget target)
	{
		target = null;
		StaticTarget[] componentsInScene = CurrentRoom.SceneHandle.GetComponentsInScene<StaticTarget>();
		if (componentsInScene.Length == 0)
		{
			return false;
		}
		float num = float.MaxValue;
		StaticTarget[] array = componentsInScene;
		foreach (StaticTarget staticTarget in array)
		{
			Vector3 position = staticTarget.transform.position;
			float num2 = Vector2.Distance(dronePosition, position);
			if (!(num2 >= radius) && !(num2 >= num))
			{
				num = num2;
				target = staticTarget;
			}
		}
		return target != null;
	}
}
