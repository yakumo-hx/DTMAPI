using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedSaw.AI.StateMachine;

public class StateMachine
{
	private readonly Dictionary<Type, State> states = new Dictionary<Type, State>();

	private readonly Type defaultType;

	public State currentState { get; private set; }

	public StateMachine(string name, object[] args)
	{
		Type[] array = ReflectionUtils.FindTypesWithAttributeInExecutingAsm<StateAttribute>(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		object[] array2 = new object[args.Length + 1];
		args.CopyTo(array2, 0);
		array2[args.Length] = this;
		Type[] array3 = array;
		foreach (Type type in array3)
		{
			if (CheckType(type, name, out var isDefault))
			{
				states.Add(type, (State)Activator.CreateInstance(type, array2));
				if (isDefault && defaultType == null)
				{
					defaultType = type;
				}
			}
		}
		if (defaultType == null && states.Count > 0)
		{
			defaultType = states.First().Key;
		}
	}

	private static bool CheckType(Type type, string controllerName, out bool isDefault)
	{
		isDefault = false;
		StateAttribute[] array = type.GetCustomAttributes<StateAttribute>().ToArray();
		foreach (StateAttribute stateAttribute in array)
		{
			if (!(stateAttribute.aiName != controllerName))
			{
				isDefault = stateAttribute.isDefault;
				return true;
			}
		}
		return false;
	}

	public State GetState(Type type)
	{
		return states.GetValueOrDefault(type, null);
	}

	protected virtual void OnStateChanged(State nextState)
	{
	}

	private void __set_state(State nextState)
	{
		currentState?.OnExit();
		currentState = nextState;
		currentState.OnEnter();
		OnStateChanged(currentState);
	}

	public void ChangeState<T>(Action<T> handle = null) where T : State
	{
		if (states.TryGetValue(typeof(T), out var value))
		{
			handle?.Invoke((T)value);
			__set_state(value);
		}
	}

	public void Update(float dt)
	{
		if (states.Count == 0)
		{
			return;
		}
		if (currentState == null)
		{
			if (defaultType != null)
			{
				__set_state(states[defaultType]);
			}
			return;
		}
		State nextState = currentState.GetNextState(dt);
		if (nextState != null)
		{
			__set_state(nextState);
		}
	}

	public void FixedUpdate(float dt)
	{
		currentState?.FixedUpdate(dt);
	}
}
