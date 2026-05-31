using System;
using System.Collections.Generic;
using DolocTown.NodeCanvas;
using UnityEngine;

namespace DolocTown;

public class GameProcessSystem
{
	private readonly Dictionary<GameProcessGraph, GameProcessRunner> _runners = new Dictionary<GameProcessGraph, GameProcessRunner>();

	private bool _messageProtected;

	private bool _updateProtected;

	private readonly Queue<GameProcessGraph> _toRemoveGraphs = new Queue<GameProcessGraph>();

	private void FlushToRemoveGraphs()
	{
		while (_toRemoveGraphs.Count > 0)
		{
			GameProcessGraph key = _toRemoveGraphs.Dequeue();
			if (_runners.ContainsKey(key))
			{
				_runners.Remove(key);
			}
		}
	}

	public void OnUpdate(float dt)
	{
		if (_runners.Count == 0)
		{
			return;
		}
		_updateProtected = true;
		foreach (GameProcessRunner value in _runners.Values)
		{
			value.OnUpdate(dt);
		}
		_updateProtected = false;
		FlushToRemoveGraphs();
	}

	public void SendMessage(GameMessage message)
	{
		if (_runners.Count == 0)
		{
			return;
		}
		_messageProtected = true;
		foreach (GameProcessRunner value in _runners.Values)
		{
			value.SendMessage(message);
		}
		_messageProtected = false;
		FlushToRemoveGraphs();
	}

	public void StopProcess(GameProcessGraph graph)
	{
		if (_runners.TryGetValue(graph, out var value))
		{
			value.StopProcess();
			if (!_messageProtected && !_updateProtected)
			{
				_runners.Remove(graph);
			}
			else
			{
				_toRemoveGraphs.Enqueue(graph);
			}
		}
	}

	public bool IsProcessRunning(GameProcessGraph graph)
	{
		return _runners.ContainsKey(graph);
	}

	public bool RunProcess(GameProcessGraph graph, Action callback = null)
	{
		if (graph == null)
		{
			return false;
		}
		if (graph.OriginalGraph != null)
		{
			graph = graph.OriginalGraph;
		}
		if (_runners.ContainsKey(graph))
		{
			return false;
		}
		GameProcessRunner gameProcessRunner = DolocAPI.EntitySystem.Next<GameProcessRunner>();
		if (gameProcessRunner == null)
		{
			Debug.LogError("流程运行器创建失败..");
			return false;
		}
		gameProcessRunner.RunProcess(graph, callback);
		_runners.Add(graph, gameProcessRunner);
		return true;
	}

	public void Clear()
	{
		Queue<GameProcessRunner> queue = new Queue<GameProcessRunner>();
		foreach (GameProcessRunner value in _runners.Values)
		{
			value.StopProcess(shouldRecycle: false);
			queue.Enqueue(value);
		}
		_runners.Clear();
		while (queue.Count > 0)
		{
			DolocAPI.EntitySystem.Recycle(queue.Dequeue());
		}
	}
}
