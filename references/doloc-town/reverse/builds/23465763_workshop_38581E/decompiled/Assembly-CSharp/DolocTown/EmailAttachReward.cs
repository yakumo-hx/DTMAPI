using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class EmailAttachReward : EmailAttachBase
{
	[JsonProperty]
	public readonly Reward reward;

	[JsonProperty]
	private bool isAccept;

	public override bool HasAttach => true;

	public override bool IsAccept => isAccept;

	public string RewardInfo => reward.RewardInfo;

	public string RewardBriefInfo => reward.RewardBriefInfo;

	public Sprite RewardIcon => reward.RewardIcon;

	public int RewardCount => reward.Count;

	public EmailAttachReward(RewardProto proto)
	{
		reward = Reward.CreateReward(proto);
	}

	public EmailAttachReward(Reward reward)
	{
		this.reward = reward;
	}

	[JsonConstructor]
	private EmailAttachReward(Reward reward, bool isAccept)
	{
		this.reward = reward;
		this.isAccept = isAccept;
	}

	public override void OnFirstRead()
	{
	}

	public override bool OnAccept()
	{
		if (isAccept)
		{
			return true;
		}
		isAccept = reward.CashReward();
		return isAccept;
	}

	public override string ToString()
	{
		return "奖励附件:" + RewardInfo;
	}
}
