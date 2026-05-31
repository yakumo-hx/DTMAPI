using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("执行控制台命令", 0)]
[Category("多洛可小镇")]
[Description("执行一条控制台命令")]
public class DialogueTask_RunCommand : DialogueTask
{
	[SerializeField]
	private List<string> commands = new List<string>();

	public override string taskTitle
	{
		get
		{
			if (commands == null || commands.Count <= 0)
			{
				return "执行控制台命令: (无)";
			}
			return "执行控制台命令\n" + string.Join("\n", commands.Select((string c) => "\"" + c + "\""));
		}
	}

	public override void DoAction(Graph graph)
	{
		if (commands == null || commands.Count == 0)
		{
			return;
		}
		foreach (string command in commands)
		{
			if (!string.IsNullOrWhiteSpace(command))
			{
				Debug.Log("任务节点执行命令: " + command);
				DolocAPI.ExecuteCommand(command, out var _);
			}
		}
	}
}
