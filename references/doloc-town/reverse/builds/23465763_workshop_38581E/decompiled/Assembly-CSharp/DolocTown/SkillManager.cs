using System.Collections.Generic;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class SkillManager
{
	private readonly SkillProto proto;

	private readonly NashObjectPool<Skill> pool;

	private readonly List<Skill> skills = new List<Skill>();

	private readonly Queue<Skill> recycleQueue = new Queue<Skill>();

	private int referenceCount;

	public string SkillId => proto.id;

	public SkillManager(Transform container, SkillProto proto)
	{
		this.proto = proto;
		pool = new NashObjectPool<Skill>(proto.prefab, container);
	}

	public void AddReference()
	{
		referenceCount++;
	}

	public bool RemoveReference()
	{
		return --referenceCount > 0;
	}

	public void Dispose()
	{
		skills.Clear();
		pool.RecycleAll();
		pool.DestroyGarbage();
		referenceCount = 0;
	}

	public void OnFixedUpdate(float dt)
	{
		foreach (Skill skill2 in skills)
		{
			if (skill2.OnFixedUpdate(dt))
			{
				recycleQueue.Enqueue(skill2);
			}
		}
		while (recycleQueue.Count > 0)
		{
			Skill skill = recycleQueue.Dequeue();
			skills.Remove(skill);
			pool.Recycle(skill);
		}
	}

	public bool Raise(out Skill skill)
	{
		skill = pool.Next;
		if (skill == null)
		{
			return false;
		}
		if (skill.OnStart())
		{
			skills.Add(skill);
			return true;
		}
		pool.Recycle(skill);
		return false;
	}

	public void ClearSkills()
	{
		skills.Clear();
		pool.RecycleAll();
		pool.DestroyGarbage();
	}

	public void OnPauseGame()
	{
		foreach (Skill skill in skills)
		{
			skill.OnGamePaused();
		}
	}

	public void OnResumeGame()
	{
		foreach (Skill skill in skills)
		{
			skill.OnGameResumed();
		}
	}
}
