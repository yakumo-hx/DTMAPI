using DolocTown.NodeCanvas;
using UnityEngine;

namespace DolocTown.GameData;

public class TransactionScheduleConnection : HorizontalLinkedConnection
{
	[SerializeField]
	private bool hasCondition;

	[SerializeField]
	private DialogueConditionTask condition;

	public bool IsConditionMet
	{
		get
		{
			if (!hasCondition || condition == null)
			{
				return true;
			}
			return condition.IsConditionMet;
		}
	}

	public string ConditionInfo
	{
		get
		{
			if (!hasCondition || condition == null)
			{
				return "";
			}
			return "\n 如果 <size=12><color=#fffde3>" + condition.taskTitle + "</color></size>";
		}
	}
}
