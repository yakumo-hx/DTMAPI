using DolocTown.NodeCanvas;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.GameData;

[Name("事务节点", 0)]
[Description("设定一个条件，当条件符合时执行其后续的节点，和任务不同的是，它的监听是永久的")]
public class RoutineNode_Trigger : RoutineNode
{
	[SerializeField]
	private MissionEvent triggerEvent = new MissionEvent();

	[SerializeField]
	private bool hasAdditionalCondition;

	[SerializeField]
	private DialogueConditionTask condition;

	public override string name => "事务节点";

	public override int maxInConnections => 1;

	public override int maxOutConnections => -1;

	protected override bool CanConnectFromSource(Node sourceNode)
	{
		return sourceNode is RoutineNodeEntrance;
	}

	public void SendMessage(GameMessage message)
	{
		if (base.outConnections.Count == 0 || !triggerEvent.CheckMessage(message.Type, message.Args, out var _, out var _))
		{
			return;
		}
		if (hasAdditionalCondition)
		{
			DialogueConditionTask dialogueConditionTask = condition;
			if (dialogueConditionTask != null && !dialogueConditionTask.IsConditionMet)
			{
				return;
			}
		}
		foreach (Connection outConnection in base.outConnections)
		{
			if (!(outConnection is TransactionScheduleConnection { IsConditionMet: not false }))
			{
				continue;
			}
			Node targetNode = outConnection.targetNode;
			if (!(targetNode is RoutineNode_Action routineNode_Action))
			{
				if (targetNode is RoutineNode_Dialogue routineNode_Dialogue)
				{
					routineNode_Dialogue.Execute();
				}
			}
			else
			{
				routineNode_Action.Execute();
			}
		}
	}
}
