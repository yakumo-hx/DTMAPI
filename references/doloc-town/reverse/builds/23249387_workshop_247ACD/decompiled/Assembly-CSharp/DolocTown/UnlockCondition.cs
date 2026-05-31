using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public class UnlockCondition : ICondition
{
	[SerializeField]
	private string lockableObjectId;

	public bool IsConditionMet(bool reverseCondition)
	{
		if (lockableObjectId.IsNullOrEmpty())
		{
			return true;
		}
		return !DolocAPI.archiveHandle.cityData.globalInteractableObjectManager.LoadLockState(lockableObjectId) != reverseCondition;
	}

	public override string ToString()
	{
		return "解锁<" + lockableObjectId + ">对象";
	}
}
