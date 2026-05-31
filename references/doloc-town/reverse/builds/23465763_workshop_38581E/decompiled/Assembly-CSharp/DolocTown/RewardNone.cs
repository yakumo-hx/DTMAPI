using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown;

public class RewardNone : Reward
{
	public override Sprite RewardIcon => LocSprites.UI_ITEMICON_DEFAULT;

	public override int Count => 0;

	public override string RewardBriefInfo => "";

	public override string RewardInfo => "";

	public RewardNone()
		: base(RewardType.NONE)
	{
	}

	public override bool CheckValid()
	{
		return true;
	}

	public override bool CashReward()
	{
		return true;
	}

	public override bool TryCombine(Reward other, out Reward combined)
	{
		if (other is RewardNone)
		{
			combined = new RewardNone();
			return true;
		}
		combined = null;
		return false;
	}
}
