using UnityEngine;

namespace DolocTown;

public class AgentWallPassByChecker : AgentTouchCheckerBase
{
	public override bool Check(GameObject other)
	{
		return other.name == "wall";
	}
}
