using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Email;
using UnityEngine;

namespace DolocTown.UI;

public struct EmailData : IUIData
{
	public string title;

	public string content;

	public string sender;

	public bool isNew;

	public bool isAccept;

	public bool hasReward;

	public bool isCollect;

	public bool hasUnReceivedReward;

	public Sprite closedEmailSprite;

	public Sprite openEmailSprite;

	public RewardAcceptState[] rewardStates;

	public string rewardText;

	public string buttonText;

	public string hintText;

	public string timeText;

	public bool notEmpty { get; }

	public EmailData(Email email)
	{
		notEmpty = true;
		title = email.Title;
		content = email.Content;
		sender = email.Sender;
		isNew = email.IsNew;
		isAccept = email.IsAccept;
		buttonText = string.Empty;
		hintText = string.Empty;
		hasReward = false;
		isCollect = email.Collected;
		hasUnReceivedReward = false;
		timeText = email.SendTime;
		rewardStates = null;
		rewardText = string.Empty;
		closedEmailSprite = DolocAPI.LoadSprite(email.IconUrl + "_close");
		openEmailSprite = DolocAPI.LoadSprite(email.IconUrl + "_open");
		if (!email.HasAttach)
		{
			return;
		}
		if (email.HasUnReceivedMission)
		{
			buttonText = DolocConfig.StaticTexts.UiEmailAcceptMission;
		}
		else if (email.HasUnReceivedReward)
		{
			buttonText = DolocConfig.StaticTexts.UiEmailReciveItem;
		}
		else
		{
			CfgEmailAttachBase[] attaches = email.proto.Attaches;
			for (int i = 0; i < attaches.Length; i++)
			{
				if (attaches[i] is CfgEmailAttachReward)
				{
					hintText = DolocConfig.StaticTexts.UiEmailAlreadyRecived;
					break;
				}
			}
		}
		hasUnReceivedReward = email.HasUnReceivedReward;
		List<RewardAcceptState> rewardsStates = email.RewardsStates;
		hasReward = rewardsStates.Count != 0;
		if (hasReward)
		{
			rewardStates = rewardsStates.ToArray();
			rewardText = string.Join(", ", rewardsStates.Select((RewardAcceptState x) => x.title));
		}
	}
}
