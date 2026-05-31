using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct ConditionGroupChecker
{
	[SerializeField]
	private TextTipConfig defaultConditionFailedText;

	[SerializeField]
	private ConditionChecker[] conditionChecker;

	[SerializeField]
	private ConditionMode conditionMode;

	public bool IsConditionMet(out TextTipConfig conditionFailedText)
	{
		conditionFailedText = default(TextTipConfig);
		if (this.conditionChecker.IsNullOrEmpty())
		{
			return true;
		}
		switch (conditionMode)
		{
		case ConditionMode.All:
		{
			ConditionChecker[] array = this.conditionChecker;
			foreach (ConditionChecker conditionChecker3 in array)
			{
				if (!conditionChecker3.IsConditionMet(out conditionFailedText))
				{
					if (conditionFailedText.isEmpty)
					{
						conditionFailedText = defaultConditionFailedText;
					}
					return false;
				}
			}
			return true;
		}
		case ConditionMode.Any:
		{
			ConditionChecker[] array = this.conditionChecker;
			foreach (ConditionChecker conditionChecker2 in array)
			{
				if (conditionChecker2.IsConditionMet(out conditionFailedText))
				{
					return true;
				}
				if (conditionFailedText.isEmpty)
				{
					conditionFailedText = defaultConditionFailedText;
				}
			}
			return false;
		}
		case ConditionMode.None:
		{
			ConditionChecker[] array = this.conditionChecker;
			foreach (ConditionChecker conditionChecker in array)
			{
				if (conditionChecker.IsConditionMet(out conditionFailedText))
				{
					if (conditionFailedText.isEmpty)
					{
						conditionFailedText = defaultConditionFailedText;
					}
					return false;
				}
			}
			return true;
		}
		default:
			return false;
		}
	}

	public override string ToString()
	{
		string text = string.Empty;
		ConditionChecker[] array = this.conditionChecker;
		for (int i = 0; i < array.Length; i++)
		{
			ConditionChecker conditionChecker = array[i];
			text = text + conditionChecker.ToString() + "\t";
		}
		return text;
	}
}
