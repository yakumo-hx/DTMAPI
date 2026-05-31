using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct MissionCondition : ICondition
{
	[SerializeField]
	private string missionChainId;

	[SerializeField]
	private string missionId;

	public bool IsConditionMet(bool reverseCondition)
	{
		if (missionId.IsNullOrEmpty())
		{
			return true;
		}
		return DolocAPI.IsMissionComplete(missionId) != reverseCondition;
	}

	public override string ToString()
	{
		return "完成<" + missionId + ">任务";
	}
}
