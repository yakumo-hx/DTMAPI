using UnityEngine;

namespace DolocTown.UI;

public struct RewardAcceptState
{
	public Sprite icon;

	public int count;

	public string title;

	public bool isAccept;

	public RewardAcceptState(EmailAttachReward attachReward)
	{
		title = attachReward.RewardBriefInfo;
		icon = attachReward.RewardIcon;
		count = attachReward.RewardCount;
		isAccept = attachReward.IsAccept;
	}
}
