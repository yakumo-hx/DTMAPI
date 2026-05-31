using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct EventDecoratorCondition : ICondition
{
	[SerializeField]
	private string eventDecoratorId;

	public bool IsConditionMet(bool reverseCondition)
	{
		if (eventDecoratorId.IsNullOrEmpty())
		{
			return true;
		}
		return DolocAPI.IsEventDecoratorComplete(eventDecoratorId) != reverseCondition;
	}

	public override string ToString()
	{
		return "完成<" + eventDecoratorId + ">事件装饰器";
	}
}
