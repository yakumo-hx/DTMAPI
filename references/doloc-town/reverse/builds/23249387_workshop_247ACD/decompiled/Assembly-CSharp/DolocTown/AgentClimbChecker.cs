using UnityEngine;

namespace DolocTown;

public class AgentClimbChecker : AgentTouchCheckerBase
{
	public override bool Check(GameObject other)
	{
		if (other.name != "wall")
		{
			return false;
		}
		return CheckState<AgentStateClimb>();
	}
}
