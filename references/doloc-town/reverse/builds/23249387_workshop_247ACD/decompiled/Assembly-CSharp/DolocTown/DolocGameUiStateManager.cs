using System;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class DolocGameUiStateManager
{
	private LRUCache<Type, DolocGameUiState> stateLruCache;

	public DolocGameUiStateManager(int lruCacheCapacity = 20)
	{
		stateLruCache = new LRUCache<Type, DolocGameUiState>(lruCacheCapacity, OnRemoveState);
	}

	[Command("view_ui_states")]
	private static void Show()
	{
		int num = 0;
		foreach (DolocGameUiState value in DolocAPI.gameUiStates.stateLruCache.Values)
		{
			Debug.Log($"{num++}: {value.GetType().Name}");
		}
	}

	public bool HasState<T>() where T : DolocGameUiState
	{
		return stateLruCache.ContainsKey(typeof(T));
	}

	public T GetState<T>() where T : DolocGameUiState
	{
		Type typeFromHandle = typeof(T);
		if (!stateLruCache.ContainsKey(typeFromHandle))
		{
			T val = Activator.CreateInstance<T>();
			stateLruCache.Add(typeFromHandle, val, val.PermanentState);
			val.Init();
		}
		return stateLruCache[typeFromHandle] as T;
	}

	private void OnRemoveState(DolocGameUiState state)
	{
		state.Destroy();
	}

	public DolocGameUiState GetStateByName(string uiStateName)
	{
		Type type = Type.GetType(uiStateName);
		if (!typeof(DolocGameUiState).IsAssignableFrom(type))
		{
			Debug.LogError("状态类型<" + uiStateName + ">不是DolocGameUiState的子类");
			return null;
		}
		return (DolocGameUiState)((typeof(DolocGameUiStateManager).GetMethod("GetState", new Type[0])?.MakeGenericMethod(type))?.Invoke(this, null));
	}

	public T EnterUI<T>() where T : DolocGameUiState
	{
		return EnterUI<T>(null);
	}

	public T EnterUI<T>(Func<T, bool> handleStartUpArgs) where T : DolocGameUiState
	{
		T state = GetState<T>();
		if (handleStartUpArgs != null && !handleStartUpArgs(state))
		{
			return null;
		}
		state.Startup();
		return state;
	}

	public void RemoveUI<T>() where T : DolocGameUiState
	{
		stateLruCache.Remove(typeof(T));
	}

	public void Access(DolocGameUiState state)
	{
		stateLruCache.TryGetValue(state.GetType(), out var _);
	}
}
