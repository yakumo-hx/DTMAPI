using System;
using System.Collections.Generic;
using DolocTown;
using UnityEngine;

public class GameStateMachine
{
	private readonly Stack<IDolocGameState> states;

	private readonly Dictionary<Type, GameStatePlugin> plugins;

	private readonly Queue<Type> pluginRecycleQueue;

	private IDolocGameState currentState;

	private bool IsProtectedMode;

	private Action<float> __onUpdate;

	private Action<float> __onLateUpdate;

	private Action<float> __onFixedUpdate;

	public IDolocGameState CurrentState => currentState;

	public IDolocGameState NextGameState { get; private set; }

	public IEnumerable<string> stateStack
	{
		get
		{
			foreach (IDolocGameState state in states)
			{
				yield return state.GetType().Name;
			}
		}
	}

	public GameStateMachine()
	{
		DolocAPI.UserInput.BindDeviceChangedCallback(delegate(DolocInputDeviceType type)
		{
			currentState?.OnInputDeviceChanged(type);
		});
		states = new Stack<IDolocGameState>();
		currentState = null;
		__onUpdate = EmptyLoop;
		__onFixedUpdate = EmptyLoop;
		__onLateUpdate = EmptyLoop;
		plugins = new Dictionary<Type, GameStatePlugin>();
		pluginRecycleQueue = new Queue<Type>();
		IsProtectedMode = false;
	}

	public void Update(float deltaTime)
	{
		__onUpdate(deltaTime);
	}

	public void FixedUpdate(float deltaTime)
	{
		__onFixedUpdate(deltaTime);
	}

	public void LateUpdate(float deltaTime)
	{
		__onLateUpdate(deltaTime);
	}

	private void EmptyLoop(float _)
	{
	}

	private void SetCurrentInput(DolocInputType type)
	{
		DolocAPI.UserInput.InputType = type;
	}

	private void SetState(IDolocGameState state)
	{
		currentState = state;
		if (state != null)
		{
			__onUpdate = currentState.OnUpdate;
			__onFixedUpdate = currentState.OnFixedUpdate;
			if (currentState.ShouldLateUpdate)
			{
				__onLateUpdate = currentState.OnLateUpdate;
			}
			else
			{
				__onLateUpdate = EmptyLoop;
			}
			SetCurrentInput(state.InputType);
			state.OnInputDeviceChanged(DolocAPI.UserInput.DeviceType);
			DolocAPI.gameLoop.IsPaused = state.ShouldPauseGame;
		}
		else
		{
			__onUpdate = EmptyLoop;
			__onFixedUpdate = EmptyLoop;
			__onLateUpdate = EmptyLoop;
			SetCurrentInput(DolocInputType.NONE);
			DolocAPI.gameLoop.IsPaused = true;
		}
	}

	public void PushState(IDolocGameState nextState)
	{
		if (!IsProtectedMode && nextState != null && currentState != nextState && !states.Contains(nextState))
		{
			IsProtectedMode = true;
			IDolocGameState dolocGameState = currentState;
			NextGameState = nextState;
			BeforeSwitchState(nextState, dolocGameState);
			if (dolocGameState != null)
			{
				dolocGameState.__pause();
				states.Push(dolocGameState);
			}
			nextState.__enter();
			SetState(nextState);
			IsProtectedMode = false;
			AfterSwitchState(nextState, dolocGameState);
		}
	}

	public bool PopState()
	{
		if (IsProtectedMode)
		{
			return false;
		}
		if (currentState == null)
		{
			return false;
		}
		IsProtectedMode = true;
		IDolocGameState dolocGameState = currentState;
		IDolocGameState dolocGameState3 = (NextGameState = ((states.Count > 0) ? states.Pop() : null));
		BeforeSwitchState(dolocGameState3, dolocGameState);
		dolocGameState.__exit();
		dolocGameState3?.__resume();
		SetState(dolocGameState3);
		IsProtectedMode = false;
		AfterSwitchState(dolocGameState3, dolocGameState);
		return true;
	}

	public bool TryPopState(IDolocGameState state)
	{
		if (state != currentState)
		{
			return false;
		}
		return PopState();
	}

	public void WaitToPopState(IDolocGameState state)
	{
		if (!TryPopState(state))
		{
			DolocAPI.WaitUntil(() => CurrentState == state || !TryGetStateInStack(state), delegate
			{
				TryPopState(state);
			});
		}
	}

	private void ClearStateOld(IDolocGameState state)
	{
		if (!IsProtectedMode)
		{
			states.Clear();
			if (currentState != null)
			{
				IsProtectedMode = true;
				IDolocGameState dolocGameState = currentState;
				BeforeSwitchState(state, dolocGameState);
				dolocGameState.__exit();
				state?.__enter();
				SetState(state);
				IsProtectedMode = false;
				AfterSwitchState(state, dolocGameState);
			}
			else
			{
				IsProtectedMode = true;
				BeforeSwitchState(state, null);
				state?.__enter();
				SetState(state);
				IsProtectedMode = false;
				AfterSwitchState(state, null);
			}
		}
	}

	public void ClearState(IDolocGameState state)
	{
		if (!IsProtectedMode)
		{
			int num = 100;
			while (states.Count > 0 && num-- > 0)
			{
				PopState();
			}
			IsProtectedMode = true;
			BeforeSwitchState(state, null);
			state?.__enter();
			SetState(state);
			IsProtectedMode = false;
			AfterSwitchState(state, null);
		}
	}

	public bool CheckState<T>() where T : IDolocGameState
	{
		return currentState is T;
	}

	public bool CheckState(IDolocGameState state)
	{
		return currentState == state;
	}

	public bool TryGetStateInStack(IDolocGameState state)
	{
		foreach (IDolocGameState state2 in states)
		{
			if (state == state2)
			{
				return true;
			}
		}
		return false;
	}

	private void BeforeSwitchState(IDolocGameState cur, IDolocGameState lst)
	{
		foreach (GameStatePlugin value in plugins.Values)
		{
			try
			{
				value.BeforeSwitch(lst, cur);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}

	private void AfterSwitchState(IDolocGameState cur, IDolocGameState lst)
	{
		foreach (GameStatePlugin value in plugins.Values)
		{
			try
			{
				value.AfterSwitch(lst, cur);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		ClearRecyclePlugins();
	}

	private void ClearRecyclePlugins()
	{
		while (pluginRecycleQueue.Count > 0)
		{
			plugins.Remove(pluginRecycleQueue.Dequeue());
		}
	}

	public void InstallPlugin(GameStatePlugin p)
	{
		if (!plugins.ContainsKey(p.Id))
		{
			plugins.Add(p.Id, p);
		}
	}

	public void RemovePlugin<T>() where T : GameStatePlugin
	{
		Type typeFromHandle = typeof(T);
		if (plugins.ContainsKey(typeFromHandle))
		{
			pluginRecycleQueue.Enqueue(typeFromHandle);
		}
	}

	public T GetPlugin<T>() where T : GameStatePlugin
	{
		Type typeFromHandle = typeof(T);
		if (plugins.ContainsKey(typeFromHandle))
		{
			return plugins[typeFromHandle] as T;
		}
		return null;
	}

	public override string ToString()
	{
		if (currentState != null)
		{
			return $"{currentState}, 栈区空间:{states.Count}";
		}
		return "暂无控制器";
	}
}
