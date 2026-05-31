using System;
using ParadoxNotion;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct LikabilityCondition : ICondition
{
	[SerializeField]
	private string npcName;

	[SerializeField]
	private int targetLevel;

	[SerializeField]
	private CompareMethod compareMethod;

	public bool IsConditionMet(bool reverseCondition)
	{
		return OperationTools.Compare(DolocAPI.QueryNpcLikingLv(npcName), targetLevel, compareMethod) != reverseCondition;
	}

	public override string ToString()
	{
		return $"{npcName}好感度{OperationTools.GetCompareString(compareMethod)}{targetLevel}";
	}
}
