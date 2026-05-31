using System;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("功能节点", 0)]
[Description("执行一个函数功能")]
[ParadoxNotion.Design.Icon("Action", false, "")]
public class MissionNodeAction : MissionNode, ITaskAssignable<DialogueTask>, ITaskAssignable, IGraphElement
{
	[SerializeField]
	private DialogueTask _action;

	public override MissionNodeType nodeType => MissionNodeType.ACTION;

	public override Alignment2x2 iconAlignment => Alignment2x2.Default;

	public override Alignment2x2 commentsAlignment => Alignment2x2.Bottom;

	public override string name
	{
		get
		{
			if (_action != null)
			{
				return _action.taskTitle;
			}
			return "功能节点(未设置)";
		}
	}

	public override int maxOutConnections => 0;

	public override int maxInConnections => -1;

	public DialogueTask action
	{
		get
		{
			return _action;
		}
		set
		{
			_action = value;
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
			action = (DialogueTask)value;
		}
	}

	public override Type outConnectionType => typeof(MissionConnection);

	public void Execute()
	{
		action?.DoAction(base.graph);
	}
}
