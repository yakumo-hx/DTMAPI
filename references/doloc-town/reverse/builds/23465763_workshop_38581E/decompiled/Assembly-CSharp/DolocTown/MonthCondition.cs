using System;
using ParadoxNotion;
using UnityEngine;

namespace DolocTown;

[Serializable]
public class MonthCondition : ICondition
{
	[SerializeField]
	private int month;

	[SerializeField]
	private CompareMethod compareMethod;

	public bool IsConditionMet(bool reverseCondition)
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.timeData.dateNow.Month, month, compareMethod) != reverseCondition;
	}

	public override string ToString()
	{
		return $"月份{OperationTools.GetCompareString(compareMethod)}{month}";
	}
}
