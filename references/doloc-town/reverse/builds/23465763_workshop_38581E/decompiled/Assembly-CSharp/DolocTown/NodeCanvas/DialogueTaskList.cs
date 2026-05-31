using System.Collections.Generic;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇")]
[Name("功能组", 0)]
[Description("允许静态执行一组功能节点")]
public class DialogueTaskList : DialogueTask
{
	[SerializeField]
	public List<DialogueTask> actions = new List<DialogueTask>();

	public override string taskTitle
	{
		get
		{
			if (actions.Count == 0)
			{
				return "No Actions";
			}
			string text = string.Empty;
			for (int i = 0; i < actions.Count; i++)
			{
				DialogueTask dialogueTask = actions[i];
				if (dialogueTask != null && dialogueTask.isUserEnabled)
				{
					string text2 = (dialogueTask.isPaused ? "<b>||</b> " : (dialogueTask.isRunning ? "► " : "▪"));
					text = text + text2 + dialogueTask.summaryInfo + ((i == actions.Count - 1) ? "" : "\n");
				}
			}
			return text;
		}
	}

	public override Task Duplicate(ITaskSystem newOwnerSystem)
	{
		DialogueTaskList dialogueTaskList = (DialogueTaskList)base.Duplicate(newOwnerSystem);
		dialogueTaskList.actions.Clear();
		foreach (DialogueTask action in actions)
		{
			dialogueTaskList.AddAction((DialogueTask)action.Duplicate(newOwnerSystem));
		}
		return dialogueTaskList;
	}

	public override void DoAction(Graph graph)
	{
		foreach (DialogueTask action in actions)
		{
			action.DoAction(graph);
		}
	}

	public override void OnDrawGizmosSelected()
	{
		for (int i = 0; i < actions.Count; i++)
		{
			if (actions[i].isUserEnabled)
			{
				actions[i].OnDrawGizmosSelected();
			}
		}
	}

	public void AddAction(DialogueTask action)
	{
		if (action is DialogueTaskList)
		{
			foreach (DialogueTask action2 in (action as DialogueTaskList).actions)
			{
				AddAction(action2);
			}
			return;
		}
		actions.Add(action);
		action.SetOwnerSystem(base.ownerSystem);
	}
}
