using System.Collections.Generic;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("文本组", 0)]
[Description("维护一组文本")]
public class DialogueTextTaskList : DialogueTextTask
{
	[SerializeField]
	public List<DialogueTextTask> textActions = new List<DialogueTextTask>();

	public override string taskTitle
	{
		get
		{
			string text = string.Empty;
			int num = 0;
			foreach (DialogueTextTask textAction in textActions)
			{
				text = text + num++ + "." + textAction.taskTitle + "\n";
			}
			return text.TrimEnd('\n');
		}
	}

	public int Count => textActions.Count;

	public void AddCondition(DialogueTextTask textAction)
	{
		if (textAction is DialogueTextTaskList)
		{
			foreach (DialogueTextTask textAction2 in (textAction as DialogueTextTaskList).textActions)
			{
				AddCondition(textAction2);
			}
			return;
		}
		textActions.Add(textAction);
		textAction.SetOwnerSystem(base.ownerSystem);
	}

	public override string[] GetTexts()
	{
		List<string> list = new List<string>();
		foreach (DialogueTextTask textAction in textActions)
		{
			string[] texts = textAction.GetTexts();
			foreach (string item in texts)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}
}
