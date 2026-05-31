using UnityEngine;

namespace DolocTown;

public class TrampolineTouchChecker : AgentTouchCheckerBase
{
	public bool isDropDown;

	public override bool Check(GameObject other)
	{
		bool flag = other.name == "ground" && !DolocAPI.agent.InCutscene;
		isDropDown = flag && base.playerVelocity.y <= 0f;
		return flag;
	}
}
