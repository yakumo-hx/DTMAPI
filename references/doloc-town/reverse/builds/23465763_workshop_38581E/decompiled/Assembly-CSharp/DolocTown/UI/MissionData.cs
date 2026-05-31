using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown.UI;

public struct MissionData : IUIData
{
	public string missionId;

	public string title;

	public string type;

	public Sprite typeIcon;

	public bool isComplete;

	public string sender;

	public string description;

	public string appendix;

	public string tip;

	public string tipWithStatus;

	public Sprite previewImage;

	public string timeLimit;

	public RewardData rewardData;

	public MapTipData mapTipData;

	public int typeOrder;

	public int orderInType;

	public bool notEmpty { get; }

	public MissionData(IMission mission, bool isChainComplete)
	{
		notEmpty = true;
		missionId = mission.Id;
		isComplete = mission.IsComplete && isChainComplete;
		title = mission.Title;
		description = mission.Description;
		previewImage = null;
		List<string> list = new List<string>();
		MissionInfo baseInfo = mission.BaseInfo;
		if (baseInfo != null)
		{
			foreach (KeyValuePair<string, MissionNodeInfo> item in baseInfo.NodeInfos_Index)
			{
				if (item.Key == mission.Id)
				{
					previewImage = item.Value.PreviewImage.Asset;
				}
				string descriptionAppend = item.Value.DescriptionAppend;
				if (!descriptionAppend.IsNullOrEmpty())
				{
					string text = "· ".ClearNoWrapSpace() + descriptionAppend;
					if (item.Key == mission.Id)
					{
						list.Add(text.Colored(DolocUiColor.TEXTCOLOR_STD));
						if (!DolocAPI.gameManager.gameInitConfig.showAllMissionAppendDescription)
						{
							break;
						}
					}
					list.Add(text);
				}
				if (item.Key == mission.Id && !DolocAPI.gameManager.gameInitConfig.showAllMissionAppendDescription)
				{
					break;
				}
			}
		}
		appendix = string.Join("\n", list);
		sender = mission.Sender;
		tip = mission.Tip.ClearRichTextLabel();
		string briefStatus = mission.BriefStatus;
		tipWithStatus = (briefStatus.IsNullOrEmpty() ? tip : (mission.Tip.ClearRichTextLabel() + " " + mission.BriefStatus)).Trim();
		if (mission.LeftTime > 0)
		{
			string str = DolocUtils.Format(DolocConfig.StaticTexts.UiMissionTimeLimit, DolocAPI.GetFormatTimeLengthByHours(mission.LeftTime));
			Color color = ((mission.LeftTime <= 6) ? DolocUiColor.SLIENTCOLOR_RED : ((mission.LeftTime <= 12) ? DolocUiColor.SLIENTCOLOR_RED : DolocUiColor.TEXTCOLOR_SLIENT));
			timeLimit = str.Colored(color);
		}
		else
		{
			timeLimit = "";
		}
		rewardData = new RewardData(mission.Rewards?.ToArray());
		mapTipData = new MapTipData(mission);
		type = baseInfo?.MissionType_Ref?.Info;
		typeIcon = baseInfo?.MissionType_Ref?.UiIcon.Asset;
		typeOrder = baseInfo?.MissionType_Ref?.Order ?? (-1000);
		orderInType = baseInfo?.OrderInType ?? (-1000);
	}

	public MissionData(MissionInfo missionInfo, bool isComplete = true)
	{
		this = default(MissionData);
		if (missionInfo == null)
		{
			return;
		}
		notEmpty = true;
		this.isComplete = isComplete;
		title = missionInfo.Title;
		description = missionInfo.Description;
		List<string> list = new List<string>();
		previewImage = null;
		foreach (KeyValuePair<string, MissionNodeInfo> item2 in missionInfo.NodeInfos_Index)
		{
			string descriptionAppend = item2.Value.DescriptionAppend;
			if (!descriptionAppend.IsNullOrEmpty())
			{
				string item = "· ".ClearNoWrapSpace() + descriptionAppend;
				list.Add(item);
			}
		}
		appendix = string.Join("\n", list);
		sender = missionInfo.Sender;
		tip = string.Empty;
		tipWithStatus = string.Empty;
		timeLimit = string.Empty;
		rewardData = default(RewardData);
		mapTipData = default(MapTipData);
		type = missionInfo.MissionType_Ref?.Info;
		typeIcon = missionInfo.MissionType_Ref?.UiIcon.Asset;
		typeOrder = missionInfo.MissionType_Ref?.Order ?? (-1000);
		orderInType = missionInfo.OrderInType;
	}
}
