using UnityEngine;

namespace DolocTown;

public class HiddenTriggerChecker : AgentTouchCheckerBase
{
	public override bool Check(GameObject other)
	{
		return other.CompareTag("HiddenTrigger");
	}
}
