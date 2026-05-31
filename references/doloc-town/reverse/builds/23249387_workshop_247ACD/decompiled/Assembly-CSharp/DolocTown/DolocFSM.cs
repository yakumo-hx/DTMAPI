using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class DolocFSM<TStateType> where TStateType : Enum
{
	protected Dictionary<TStateType, IState> states = new Dictionary<TStateType, IState>();

	private Stack<IState> stateStack = new Stack<IState>();

	public IState currentState { get; protected set; }

	public IState previousState { get; protected set; }

	public int stackCount => stateStack.Count;

	public void AddState(TStateType stateType, IState state)
	{
		if (!states.TryAdd(stateType, state))
		{
			Debug.LogError("重复添加状态: " + stateType.ToString());
		}
		state.OnInit();
	}

	public void RemoveState(TStateType stateType)
	{
		if (states.ContainsKey(stateType))
		{
			states.Remove(stateType);
		}
	}

	public void EnterState(TStateType stateType, TransitionMode mode)
	{
		if (!states.TryGetValue(stateType, out var value) || value == null)
		{
			Debug.LogError("没有设置状态实例或为空 " + stateType.ToString());
		}
		previousState = currentState;
		currentState = value;
		previousState?.OnExit();
		currentState?.OnEnter();
		switch (mode)
		{
		case TransitionMode.Replace:
		{
			stateStack.TryPop(out var _);
			break;
		}
		case TransitionMode.Clean:
			stateStack.Clear();
			break;
		}
		stateStack.Push(currentState);
		if (stateStack.Count >= 10)
		{
			Debug.LogWarning("stateStack里的状态有点太多了");
		}
	}

	public void PopState()
	{
		previousState = null;
		currentState = null;
		if (stateStack.Count != 0)
		{
			previousState = stateStack.Pop();
			previousState?.OnExit();
			if (stateStack.Count != 0)
			{
				currentState = stateStack.Pop();
				currentState?.OnEnter();
			}
		}
	}

	public IState GetState(TStateType stateType)
	{
		states.TryGetValue(stateType, out var value);
		return value;
	}
}
