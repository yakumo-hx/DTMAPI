using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("分支节点", 0)]
[Description("当条件满足时选择一个子节点执行")]
[ParadoxNotion.Design.Icon("Condition", false, "")]
public class MissionNodeCondition : MissionNode, ITaskAssignable<DialogueConditionTask>, ITaskAssignable, IGraphElement
{
	[SerializeField]
	private DialogueConditionTask _condition;

	public override MissionNodeType nodeType => MissionNodeType.CONDITION;

	public override int maxOutConnections => 2;

	public override string name
	{
		get
		{
			if (_condition != null)
			{
				return _condition.taskTitle;
			}
			return "条件锁(未设置)";
		}
	}

	public ConditionTask action
	{
		get
		{
			return _condition;
		}
		set
		{
			_condition = (DialogueConditionTask)value;
		}
	}

	public Task task
	{
		get
		{
			return action;
		}
		set
		{
			action = (ConditionTask)value;
		}
	}

	public bool CheckCondition(out MissionNode node)
	{
		node = null;
		if (_condition == null || base.outConnections.Count == 0)
		{
			return false;
		}
		if (!_condition.IsConditionMet)
		{
			return false;
		}
		node = base.outConnections[0].targetNode as MissionNode;
		return true;
	}

	public MissionNode GetNodeByCondition()
	{
		if (_condition == null || base.outConnections.Count == 0)
		{
			return null;
		}
		if (base.outConnections.Count == 1)
		{
			if (_condition.IsConditionMet)
			{
				return base.outConnections[0].targetNode as MissionNode;
			}
			return null;
		}
		if (_condition.IsConditionMet)
		{
			return base.outConnections[0].targetNode as MissionNode;
		}
		return base.outConnections[1].targetNode as MissionNode;
	}
}
