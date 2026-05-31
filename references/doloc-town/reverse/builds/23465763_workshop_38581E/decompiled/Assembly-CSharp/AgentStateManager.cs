using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;

public class AgentStateManager
{
	private readonly BodyController body;

	private readonly Dictionary<Type, AgentStateBase> states = new Dictionary<Type, AgentStateBase>();

	public AgentStateBase current { get; private set; }

	public bool IsFixed { get; private set; }

	public AgentStateManager(BodyController body)
	{
		this.body = body;
		InitStates();
		Overwrite<AgentStateIdle>();
	}

	public T GetState<T>() where T : AgentStateBase
	{
		return (T)states.GetValueOrDefault(typeof(T));
	}

	public T[] GetStates<T>() where T : AgentStateBase
	{
		Type baseType = typeof(T);
		return states.Values.Where((AgentStateBase state) => baseType.IsAssignableFrom(state.GetType())).Cast<T>().ToArray();
	}

	public bool CheckState<T>() where T : AgentStateBase
	{
		return current is T;
	}

	private void InitStates()
	{
		Type typeFromHandle = typeof(AgentStateBase);
		Type[] types = typeFromHandle.Assembly.GetTypes();
		foreach (Type type in types)
		{
			if (!type.IsAbstract && type.IsSubclassOf(typeFromHandle) && !states.ContainsKey(type))
			{
				states.Add(type, (AgentStateBase)Activator.CreateInstance(type, this, body));
			}
		}
	}

	public void __SetState(AgentStateBase state)
	{
		current = state;
	}

	public void Overwrite(AgentStateBase state, bool shouldQuit = true)
	{
		if (shouldQuit)
		{
			current?.OnExit();
		}
		current = state;
		current.OnEnter();
	}

	public void Overwrite<T>(bool shouldQuit = true) where T : AgentStateBase
	{
		if (shouldQuit)
		{
			current?.OnExit();
		}
		current = GetState<T>();
		current.OnEnter();
	}

	public void Overwrite<T>(Action<T> beforeEnter, bool shouldQuit = true) where T : AgentStateBase
	{
		if (shouldQuit)
		{
			current?.OnExit();
		}
		current = GetState<T>();
		beforeEnter?.Invoke((T)current);
		current.OnEnter();
	}

	public void FixState(Action callback)
	{
		if (!(current is AgentStateIdle) || IsFixed)
		{
			callback?.Invoke();
			return;
		}
		IsFixed = true;
		callback?.Invoke();
		DolocAPI.Delay(0.1f, delegate
		{
			IsFixed = false;
		});
	}

	public void UnsetFixed()
	{
		IsFixed = false;
	}
}
