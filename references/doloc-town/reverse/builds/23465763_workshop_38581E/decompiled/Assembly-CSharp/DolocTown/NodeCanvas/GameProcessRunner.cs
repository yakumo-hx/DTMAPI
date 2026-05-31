using System;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[GameEntityManager("/global/game_process_runner", DolocGameAssets.GAME_ENTITY_PROCESS_RUNNER, CustomManagement = true)]
public class GameProcessRunner : GameEntity
{
	private bool _isRunning;

	private Action _callback;

	private GameProcessGraph _originalGraph;

	private GameProcessGraph _graph;

	public bool RunProcess(GameProcessGraph graph, Action callback)
	{
		if (_isRunning || graph == null)
		{
			return false;
		}
		_originalGraph = graph;
		_callback = callback;
		_isRunning = true;
		_graph = UnityEngine.Object.Instantiate(graph);
		_graph.OriginalGraph = graph;
		_graph.StartGraph(this, null, Graph.UpdateMode.Manual);
		return true;
	}

	public void StopProcess(bool shouldRecycle = true)
	{
		if (!(_graph == null))
		{
			_graph.StopProcess();
			_isRunning = false;
			UnityEngine.Object.Destroy(_graph);
			_graph = null;
			_originalGraph = null;
			if (shouldRecycle)
			{
				DolocAPI.EntitySystem.Recycle(this);
			}
			_callback?.Invoke();
		}
	}

	public void SendMessage(GameMessage message)
	{
		if (message != null && !(_graph == null))
		{
			_graph.SendMessage(message);
		}
	}

	public void OnUpdate(float dt)
	{
		if (_isRunning)
		{
			_graph.UpdateGraph(dt);
			if (_graph.IsProcessComplete)
			{
				DolocAPI.GameProcessSystem.StopProcess(_originalGraph);
			}
		}
	}
}
