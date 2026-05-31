using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class MonsterAI
{
	private static readonly Dictionary<string, Type> _aiTypes = _LoadTypes();

	private static readonly Dictionary<Type, Type> _defaultTypes = _LoadDefaultTypes();

	protected readonly MonsterController _controller;

	protected MonsterStateManager StateManager { get; private set; }

	public MonsterStateManager.MonsterState CurrentState => StateManager.CurrentState;

	private static Dictionary<string, Type> _LoadTypes()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		Type[] subTypes = typeof(MonsterAI).GetSubTypes();
		foreach (Type type in subTypes)
		{
			if (!type.IsDefined(typeof(MonsterAIAttribute), inherit: false))
			{
				continue;
			}
			Attribute[] customAttributes = Attribute.GetCustomAttributes(type, typeof(MonsterAIAttribute));
			for (int j = 0; j < customAttributes.Length; j++)
			{
				MonsterAIAttribute monsterAIAttribute = (MonsterAIAttribute)customAttributes[j];
				if (!dictionary.TryAdd(monsterAIAttribute.name, type))
				{
					Debug.LogError("Duplicate AI name: " + monsterAIAttribute.name);
				}
			}
		}
		return dictionary;
	}

	private static Dictionary<Type, Type> _LoadDefaultTypes()
	{
		Dictionary<Type, Type> dictionary = new Dictionary<Type, Type>();
		Type[] subTypes = typeof(MonsterAI).GetSubTypes();
		foreach (Type type in subTypes)
		{
			if (type.IsDefined(typeof(MonsterEntranceStateAttribute), inherit: false))
			{
				MonsterEntranceStateAttribute monsterEntranceStateAttribute = (MonsterEntranceStateAttribute)Attribute.GetCustomAttribute(type, typeof(MonsterEntranceStateAttribute));
				if (!dictionary.TryAdd(type, monsterEntranceStateAttribute.type))
				{
					Debug.LogError("Duplicate default state type: " + monsterEntranceStateAttribute.type);
				}
			}
		}
		return dictionary;
	}

	public static MonsterAI Create(string name, MonsterController controller)
	{
		if (name.IsNullOrEmpty() || !_aiTypes.TryGetValue(name, out var value))
		{
			MonsterAI_Default monsterAI_Default = new MonsterAI_Default();
			monsterAI_Default.SetController(controller);
			return monsterAI_Default;
		}
		MonsterAI monsterAI = (MonsterAI)Activator.CreateInstance(value);
		if (_defaultTypes.TryGetValue(value, out var value2))
		{
			monsterAI.StateManager.SetDefaultState(value2);
		}
		monsterAI.SetController(controller);
		return monsterAI;
	}

	public T GetState<T>() where T : MonsterStateManager.MonsterState
	{
		return StateManager.GetState<T>();
	}

	public void SetController(MonsterController controller)
	{
		this.WriteReadonlyField("_controller", controller);
		StateManager = new MonsterStateManager(_controller);
		InitStates();
	}

	private void InitStates()
	{
		Type rootType = typeof(MonsterStateManager.MonsterState);
		foreach (Type item in from x in GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
			where !x.IsAbstract && rootType.IsAssignableFrom(x)
			select x)
		{
			if (!item.IsDefined(typeof(DisableStateAttribute), inherit: false))
			{
				StateManager.AddState(item);
			}
		}
	}

	public T EnterState<T>() where T : MonsterStateManager.MonsterState
	{
		return StateManager.EnterState<T>();
	}

	public bool CheckState<T>(out T value) where T : MonsterStateManager.MonsterState
	{
		if (StateManager.CurrentState is T val)
		{
			value = val;
			return true;
		}
		value = null;
		return false;
	}

	public virtual void OnStart()
	{
		StateManager.OnStart();
	}

	public virtual void OnUpdate(float dt)
	{
		StateManager.OnUpdate(dt);
	}

	public virtual void OnFixedUpdate(float dt)
	{
		StateManager.OnFixedUpdate(dt);
	}

	public virtual void OnStop()
	{
	}

	public virtual void OnHurt(Transform attacker)
	{
	}

	public virtual void OnTouchByAgent()
	{
	}

	public virtual void OnDead()
	{
	}
}
