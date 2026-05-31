using System;
using NodeCanvas.Framework;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[GraphInfo(packageName = "NodeCanvas", docsURL = "https://nodecanvas.paradoxnotion.com/documentation/", resourcesURL = "https://nodecanvas.paradoxnotion.com/downloads/", forumsURL = "https://nodecanvas.paradoxnotion.com/forums-page/")]
[CreateAssetMenu(menuName = "多洛可小镇/游戏流程")]
public class GameProcessGraph : Graph
{
	public static bool _simpleDraw = true;

	private GameProcessState _currentState;

	private GameProcessState _lastState;

	private GameProcessBroken _broken;

	public override Type baseNodeType => typeof(GameProcessNode);

	public override bool requiresAgent => false;

	public override bool requiresPrimeNode => true;

	public override bool isTree => false;

	public override bool allowBlackboardOverrides => false;

	public override bool canAcceptVariableDrops => false;

	public GameProcessGraph OriginalGraph { get; set; }

	public bool IsProcessComplete { get; private set; }

	public bool IsGraphInvalid => base.primeNode == null;

	public void SendMessage(GameMessage message)
	{
		_broken?.SendMessage(message);
		_currentState?.SendMessage(message);
	}

	protected override void OnGraphStarted()
	{
		base.OnGraphStarted();
		_broken = GetQuitCondition();
		_broken?.OnBegin();
		IsProcessComplete = false;
	}

	protected override void OnGraphUpdate()
	{
		if (IsProcessComplete)
		{
			return;
		}
		if (_currentState == null)
		{
			if (!(base.primeNode is GameProcessState gameProcessState))
			{
				IsProcessComplete = true;
				return;
			}
			gameProcessState.OnEnter();
			_currentState = gameProcessState;
			return;
		}
		TaskStatus taskStatus = _currentState.Execute(Time.deltaTime);
		if (taskStatus != 0)
		{
			_lastState?.Reset();
			_currentState.OnExit(taskStatus);
			GameProcessState nextState = _currentState.NextState;
			if (nextState == null)
			{
				StopProcess();
				return;
			}
			nextState.OnEnter();
			_lastState = _currentState;
			_currentState = nextState;
		}
	}

	public void StopProcess()
	{
		Debug.Log("流程\"" + base.name + "\"已经结束");
		Stop();
		CallQuitNode();
		IsProcessComplete = true;
		if (_currentState != null)
		{
			_currentState.Reset();
			_currentState = null;
		}
	}

	private void CallQuitNode()
	{
		foreach (Node allNode in base.allNodes)
		{
			if (allNode is GameProcessQuit gameProcessQuit)
			{
				gameProcessQuit.DoQuitActions();
				break;
			}
		}
	}

	private GameProcessBroken GetQuitCondition()
	{
		foreach (Node allNode in base.allNodes)
		{
			if (allNode is GameProcessBroken result)
			{
				return result;
			}
		}
		return null;
	}
}
