using System;
using ParadoxNotion;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct FarmLevelCondition : ICondition
{
	[SerializeField]
	private CompareMethod farmLevelCompareMethod;

	[SerializeField]
	private int targetFarmLevel;

	public bool IsConditionMet(bool reverseCondition)
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.farmLevel, targetFarmLevel, farmLevelCompareMethod) != reverseCondition;
	}

	public override string ToString()
	{
		return $"农场等级{OperationTools.GetCompareString(farmLevelCompareMethod)}{targetFarmLevel}";
	}
}
