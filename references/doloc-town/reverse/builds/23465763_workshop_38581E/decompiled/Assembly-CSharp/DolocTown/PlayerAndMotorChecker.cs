using System;
using UnityEngine;

namespace DolocTown;

public class PlayerAndMotorChecker : AgentTouchCheckerBase
{
	private Func<bool> allowMotorGetter;

	public PlayerAndMotorChecker(Func<bool> allowMotorGetter = null)
	{
		this.allowMotorGetter = allowMotorGetter ?? ((Func<bool>)(() => false));
	}

	public override bool Check(GameObject other)
	{
		if (DolocAPI.IsAgentRiding && !allowMotorGetter())
		{
			return false;
		}
		return other.CompareTag("HiddenTrigger");
	}
}
