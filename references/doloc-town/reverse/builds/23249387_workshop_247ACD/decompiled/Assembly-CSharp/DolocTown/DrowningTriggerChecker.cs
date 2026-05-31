using UnityEngine;

namespace DolocTown;

public class DrowningTriggerChecker : AgentTouchCheckerBase
{
	public override bool Check(GameObject other)
	{
		return other.CompareTag("DrowningTrigger");
	}
}
