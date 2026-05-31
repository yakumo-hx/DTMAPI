using UnityEngine;

namespace DolocTown;

public class AgentDropDownChecker : AgentTouchCheckerBase
{
	public override bool Check(GameObject other)
	{
		if (other.name != "ground")
		{
			return false;
		}
		return base.playerVelocity.y <= 0f;
	}
}
