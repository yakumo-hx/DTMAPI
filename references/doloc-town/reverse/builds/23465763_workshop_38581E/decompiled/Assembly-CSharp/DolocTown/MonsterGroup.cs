using System;
using System.Collections.Generic;

namespace DolocTown;

public abstract class MonsterGroup
{
	private readonly List<MonsterAI> monsters = new List<MonsterAI>();

	private bool isLocked;

	private bool clearFlag;

	private readonly Queue<(MonsterAI, bool)> changeQueue = new Queue<(MonsterAI, bool)>();

	public void Clear()
	{
		if (isLocked)
		{
			clearFlag = true;
			return;
		}
		clearFlag = false;
		monsters.Clear();
		changeQueue.Clear();
	}

	public void Foreach(Action<MonsterAI> handle)
	{
		isLocked = true;
		foreach (MonsterAI monster in monsters)
		{
			handle?.Invoke(monster);
		}
		isLocked = false;
		if (clearFlag)
		{
			Clear();
			return;
		}
		while (changeQueue.Count > 0)
		{
			(MonsterAI, bool) tuple = changeQueue.Dequeue();
			var (entity, _) = tuple;
			if (tuple.Item2)
			{
				Add(entity);
			}
			else
			{
				Remove(entity);
			}
		}
	}

	public virtual bool TryAddMonster(MonsterController controller)
	{
		return false;
	}

	public virtual void Add(MonsterAI entity)
	{
		if (entity == null || monsters.Contains(entity))
		{
			return;
		}
		if (isLocked)
		{
			changeQueue.Enqueue((entity, true));
			return;
		}
		monsters.Add(entity);
		if (entity is MonsterAI_Group monsterAI_Group)
		{
			monsterAI_Group.OnAddedToGroup(this);
		}
	}

	public void Remove(MonsterAI entity)
	{
		if (entity != null && monsters.Contains(entity))
		{
			if (isLocked)
			{
				changeQueue.Enqueue((entity, false));
			}
			else
			{
				monsters.Remove(entity);
			}
		}
	}

	public virtual void Dispose()
	{
		Clear();
	}
}
