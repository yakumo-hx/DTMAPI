using UnityEngine;

namespace DolocTown.NodeCanvas;

public class MissionConnection : HorizontalLinkedConnection
{
	[SerializeField]
	private bool hasCondition;

	[SerializeField]
	private StartMode _startMode;

	[SerializeField]
	private DialogueConditionTask _condition;

	public bool IsAvailable
	{
		get
		{
			if (!base.isActive)
			{
				return false;
			}
			if (hasCondition && _condition != null)
			{
				return _condition.IsConditionMet;
			}
			return true;
		}
	}

	public bool IsSequence => _startMode == StartMode.Sequence;

	public bool IsParallel => _startMode == StartMode.Parallel;

	public string ConditionInfo
	{
		get
		{
			if (!hasCondition || _condition == null)
			{
				return "";
			}
			return "\n 如果 <size=12><color=#fffde3>" + _condition.taskTitle + "</color></size>";
		}
	}
}
