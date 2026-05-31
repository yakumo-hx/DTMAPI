using System.Collections.Generic;
using System.Linq;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("条件组", 0)]
[Description("允许静态判断一组条件节点")]
public class DialogueConditionTaskList : DialogueConditionTask
{
	public enum ConditionCheckMode
	{
		SATIS_ALL,
		SATIS_ANY
	}

	[SerializeField]
	public List<DialogueConditionTask> conditions = new List<DialogueConditionTask>();

	[SerializeField]
	public ConditionCheckMode checkMode;

	public override string taskTitle
	{
		get
		{
			if (conditions.Count == 0)
			{
				return "No Conditions";
			}
			string text = ((conditions.Count > 1) ? ("(" + (allTrueRequired ? "满足以下所有" : "满足以下任意") + ")\n") : string.Empty);
			for (int i = 0; i < conditions.Count; i++)
			{
				if (conditions[i] != null && conditions[i].isUserEnabled)
				{
					string text2 = "▪";
					text = text + text2 + conditions[i].summaryInfo + ((i == conditions.Count - 1) ? "" : "\n");
				}
			}
			return text;
		}
	}

	private bool allTrueRequired => checkMode == ConditionCheckMode.SATIS_ALL;

	public int Count => conditions.Count;

	public void AddCondition(DialogueConditionTask condition)
	{
		if (condition is DialogueConditionTaskList)
		{
			foreach (DialogueConditionTask condition2 in (condition as DialogueConditionTaskList).conditions)
			{
				AddCondition(condition2);
			}
			return;
		}
		conditions.Add(condition);
		condition.SetOwnerSystem(base.ownerSystem);
	}

	protected override bool CheckCondition()
	{
		return checkMode switch
		{
			ConditionCheckMode.SATIS_ALL => conditions.All((DialogueConditionTask C) => C.IsConditionMet), 
			ConditionCheckMode.SATIS_ANY => conditions.Any((DialogueConditionTask C) => C.IsConditionMet), 
			_ => false, 
		};
	}
}
