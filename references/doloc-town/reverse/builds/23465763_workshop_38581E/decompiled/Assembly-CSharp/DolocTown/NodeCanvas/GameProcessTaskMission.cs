using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("任务", 0)]
[Description("部署指定的任务到当前的游戏流程，任务完成后流程结束，（如果任务初始化失败，则会直接被视为完成）")]
public class GameProcessTaskMission : GameProcessTask, IGameProcessMission
{
	[SerializeField]
	private Graph _graph;

	[SerializeField]
	private MissionRequireTemplate _missionRequireTemplate;

	private bool hasMissionDone;

	private MissionRequireTemplateHandle _missionHandle;

	public override string SummaryInfo => _missionRequireTemplate?.SummaryInfo ?? "No Mission Selected";

	public override string ExecutionInfo => DolocUtils.Format("状态: {0}", (_missionHandle == null) ? "<color=#ff0000>无效任务</color>" : $"<size=12>{_missionHandle}</size>");

	public GameProcessTaskMission(Graph graph)
		: base(graph)
	{
	}

	public override void OnBegin()
	{
		base.OnBegin();
		if (_missionRequireTemplate == null)
		{
			hasMissionDone = true;
			return;
		}
		hasMissionDone = false;
		_missionHandle = _missionRequireTemplate.CreateHandle();
	}

	public override void OnEnd()
	{
		base.OnEnd();
		_missionHandle = null;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (!hasMissionDone)
		{
			return TaskStatus.Executing;
		}
		return TaskStatus.Success;
	}

	public bool SendMessage(GameMessage message, bool shouldUseMessage = true)
	{
		if (_missionHandle == null)
		{
			hasMissionDone = true;
			return true;
		}
		if (_missionHandle.SendMessage(message.Type, message.Args, out var statusChanged))
		{
			message.Use();
			hasMissionDone = true;
			return true;
		}
		if (statusChanged && shouldUseMessage)
		{
			message.Use();
		}
		return false;
	}
}
