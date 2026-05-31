using UnityEngine;

namespace DolocTown;

public class GravelBlockTouchChecker : AgentTouchCheckerBase
{
	public bool groundPassBy;

	public bool isClimb => CheckState<AgentStateClimb>();

	public override bool Check(GameObject other)
	{
		string name = other.name;
		bool num = name == "ground" || name == "wall";
		if (num)
		{
			groundPassBy = other.name == "ground";
		}
		return num;
	}
}
