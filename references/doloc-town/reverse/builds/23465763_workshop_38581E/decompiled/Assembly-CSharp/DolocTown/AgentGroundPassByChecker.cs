using UnityEngine;

namespace DolocTown;

public class AgentGroundPassByChecker : AgentTouchCheckerBase
{
	public override bool Check(GameObject other)
	{
		return other.name == "ground";
	}
}
