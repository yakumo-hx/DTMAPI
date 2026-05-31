using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct FactionMissionCondition : ICondition
{
	[SerializeField]
	private string factionMissionId;

	public bool IsConditionMet(bool reverseCondition)
	{
		if (factionMissionId.IsNullOrEmpty())
		{
			return true;
		}
		return DolocAPI.IsFactionMissionComplete(factionMissionId) != reverseCondition;
	}

	public override string ToString()
	{
		return "完成<" + factionMissionId + ">势力任务";
	}
}
