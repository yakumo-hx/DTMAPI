using System;
using DolocTown.Config.Mission;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Serializable]
public class MissionRewardCfg
{
	[SerializeField]
	[RequiredField]
	public string rewardTypeString;

	[SerializeField]
	[RequiredField]
	public string content;

	[SerializeField]
	[RequiredField]
	public int count;

	public bool isUnfolded = true;

	public RewardType RewardType => rewardTypeString.ConvertToEnumOrDefault<RewardType>();

	public string editorLable => $"【{rewardTypeString}】 <{content} x {count}>";

	public MissionRewardCfg()
	{
		rewardTypeString = RewardType.NONE.ToString();
		content = "";
		count = 1;
	}

	public MissionRewardCfg(string rewardType, string content, int count)
	{
		rewardTypeString = rewardType;
		this.content = content;
		this.count = count;
	}
}
