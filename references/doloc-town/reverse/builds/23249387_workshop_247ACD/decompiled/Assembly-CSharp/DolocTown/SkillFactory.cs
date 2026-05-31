using System;
using System.Collections.Generic;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class SkillFactory
{
	private readonly Transform container;

	private readonly Dictionary<string, SkillManager> managers;

	private event Action<float> OnFixedUpdated;

	public SkillFactory(Transform container)
	{
		this.container = container;
		managers = new Dictionary<string, SkillManager>();
		OnFixedUpdated += delegate
		{
		};
	}

	public SkillManager AddManager(SkillProto proto)
	{
		SkillManager skillManager;
		if (managers.ContainsKey(proto.id))
		{
			skillManager = managers[proto.id];
		}
		else
		{
			skillManager = new SkillManager(container, proto);
			managers.Add(proto.id, skillManager);
			OnFixedUpdated += skillManager.OnFixedUpdate;
		}
		skillManager.AddReference();
		return skillManager;
	}

	public void RemoveManager(string id)
	{
		if (!id.IsNullOrEmpty() && managers.TryGetValue(id, out var value) && !value.RemoveReference())
		{
			OnFixedUpdated -= value.OnFixedUpdate;
			value.Dispose();
			managers.Remove(id);
		}
	}

	public void Clear()
	{
		Queue<string> queue = new Queue<string>();
		foreach (string key in managers.Keys)
		{
			queue.Enqueue(key);
		}
		while (queue.Count > 0)
		{
			RemoveManager(queue.Dequeue());
		}
	}

	public void ClearSkills()
	{
		foreach (SkillManager value in managers.Values)
		{
			value.ClearSkills();
		}
	}

	public void OnFixedUpdate(float dt)
	{
		this.OnFixedUpdated(dt);
	}

	public void OnPause()
	{
		foreach (SkillManager value in managers.Values)
		{
			value.OnPauseGame();
		}
	}

	public void OnResume()
	{
		foreach (SkillManager value in managers.Values)
		{
			value.OnResumeGame();
		}
	}
}
