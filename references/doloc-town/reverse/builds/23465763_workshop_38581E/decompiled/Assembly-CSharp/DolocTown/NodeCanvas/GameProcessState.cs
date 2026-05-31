using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("流程", 0)]
[Description("游戏流程的具体运行节点，通过实现流程目标来部署具体的流程内容")]
[Color("92e8c0")]
public class GameProcessState : GameProcessNode
{
	[SerializeField]
	private GameProcessTask _task;

	[SerializeField]
	private DialogueTask _taskOnEnter;

	[SerializeField]
	private DialogueTask _taskOnExit;

	private List<GameProcessConnection> _failureConnections;

	private GameProcessConnection _successConnection;

	private List<IGameProcessMission> _missionTasks;

	private GameProcessConnection NextConnection;

	private bool isUnfolded;

	public override string name
	{
		get
		{
			if (_task != null)
			{
				return _task.Name;
			}
			return "Game Process";
		}
	}

	public GameProcessState NextState { get; private set; }

	public void SendMessage(GameMessage message)
	{
		if (_missionTasks.IsNullOrEmpty())
		{
			return;
		}
		foreach (IGameProcessMission missionTask in _missionTasks)
		{
			missionTask.SendMessage(message);
		}
	}

	public void OnEnter()
	{
		_task?.OnBegin();
		OnTaskStarted(_task);
		_taskOnEnter?.DoAction(base.graph);
		_failureConnections = new List<GameProcessConnection>();
		foreach (Connection outConnection in base.outConnections)
		{
			if (outConnection is GameProcessConnection { ConditionTask: not null } gameProcessConnection && gameProcessConnection.targetNode is GameProcessState)
			{
				gameProcessConnection.ConditionTask.OnBegin();
				OnTaskStarted(gameProcessConnection.ConditionTask);
				_failureConnections.Add(gameProcessConnection);
			}
		}
		_successConnection = GetFirstConnectionOfSuccess();
		base.status = Status.Running;
		foreach (GameProcessConnection failureConnection in _failureConnections)
		{
			failureConnection.status = Status.Running;
		}
		if (_successConnection != null)
		{
			_successConnection.status = Status.Running;
		}
	}

	private void OnTaskStarted(GameProcessTask task)
	{
		if (task is IGameProcessMission item)
		{
			if (_missionTasks == null)
			{
				_missionTasks = new List<IGameProcessMission>();
			}
			_missionTasks.Add(item);
		}
	}

	private GameProcessConnection GetFirstConnectionOfSuccess()
	{
		foreach (GameProcessConnection item in base.outConnections.Cast<GameProcessConnection>())
		{
			if (item.ConditionTask == null)
			{
				return item;
			}
		}
		return null;
	}

	public void OnExit(TaskStatus outStatus)
	{
		base.status = ((outStatus != TaskStatus.Failure) ? Status.Success : Status.Failure);
		foreach (GameProcessConnection failureConnection in _failureConnections)
		{
			failureConnection.status = Status.Resting;
		}
		if (_successConnection != null)
		{
			_successConnection.status = Status.Success;
		}
		if (NextConnection != null)
		{
			NextConnection.status = ((NextConnection == _successConnection) ? Status.Success : Status.Failure);
			NextState = NextConnection.targetNode as GameProcessState;
		}
		_task?.OnEnd();
		foreach (GameProcessConnection failureConnection2 in _failureConnections)
		{
			failureConnection2.ConditionTask.OnEnd();
		}
		_taskOnExit?.DoAction(base.graph);
		_missionTasks?.Clear();
	}

	public TaskStatus Execute(float dt)
	{
		if (_task == null)
		{
			return TaskStatus.Failure;
		}
		TaskStatus taskStatus = _task.OnExecute(dt);
		if (taskStatus != 0)
		{
			NextConnection = _successConnection;
			return taskStatus;
		}
		foreach (GameProcessConnection failureConnection in _failureConnections)
		{
			if (failureConnection.ConditionTask.OnExecute(dt) == TaskStatus.Success)
			{
				NextConnection = failureConnection;
				return TaskStatus.Failure;
			}
		}
		return TaskStatus.Executing;
	}

	protected override void OnReset()
	{
		base.OnReset();
		base.status = Status.Resting;
		foreach (Connection outConnection in base.outConnections)
		{
			outConnection.status = Status.Resting;
		}
	}
}
