using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("中断", 0)]
[Description("流程中断的条件节点，如果设置了该节点，则条件触发时流程会被迫中断")]
[Color("ee3046")]
public class GameProcessBroken : GameProcessNode
{
	[SerializeField]
	private GameProcessTaskMission _quitConditionTask;

	[SerializeField]
	private DialogueTask _brokenTask;

	public override int maxInConnections => 0;

	public override int maxOutConnections => 0;

	public void OnBegin()
	{
		_quitConditionTask?.OnBegin();
	}

	public void SendMessage(GameMessage message)
	{
		if (_quitConditionTask != null && _quitConditionTask.SendMessage(message, shouldUseMessage: false))
		{
			DoBrokenActions();
			DolocAPI.StopGameProcess(base.graph.name.Replace("(Clone)", ""));
		}
	}

	public void DoBrokenActions()
	{
		_brokenTask?.DoAction(base.graph);
	}
}
