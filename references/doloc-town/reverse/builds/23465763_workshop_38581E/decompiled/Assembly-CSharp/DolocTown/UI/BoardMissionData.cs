using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;

namespace DolocTown.UI;

public class BoardMissionData : IUIData
{
	public bool notEmpty { get; }

	public string title { get; }

	public string sender { get; }

	public string type { get; }

	public string desc { get; }

	public string tip { get; }

	public RewardData rewardData { get; }

	public bool levelEnough { get; }

	public BoardMissionData(BoardMission mission)
	{
		notEmpty = false;
		if (mission != null)
		{
			notEmpty = true;
			title = string.Format(DolocConfig.StaticTexts.BoardMissionTitle, mission.MissionLv, mission.BoardMissionInfo.MissionInfo_Ref.Title);
			type = "[" + mission.BoardMissionInfo.MissionLabel_Ref.Title + "] " + GetUrgentText(mission.BoardMissionInfo.TimeLimit);
			desc = mission.BoardMissionInfo.MissionInfo_Ref.Description;
			if (DolocAPI.QueryNpc(mission.BoardMissionInfo.MissionInfo_Ref.Sender, out var npc))
			{
				sender = npc.OriginTitle;
			}
			tip = GetMissionTip(mission.BoardMissionInfo);
			rewardData = new RewardData(DolocAPI.CreateRewards(mission.Rewards)?.ToArray());
			levelEnough = DolocAPI.archiveHandle.cityData.boardMissionManager.CanTakeMission(mission.MissionLv);
		}
	}

	private string GetUrgentText(int timeLimit)
	{
		if (timeLimit <= 24)
		{
			if (timeLimit <= 0)
			{
				return DolocConfig.StaticTexts.BoardMissionUrgencyNone;
			}
			return DolocConfig.StaticTexts.BoardMissionUrgencyHigh;
		}
		if (timeLimit <= 48)
		{
			return DolocConfig.StaticTexts.BoardMissionUrgencyMiddle;
		}
		return DolocConfig.StaticTexts.BoardMissionUrgencyLow;
	}

	private string GetMissionTip(BoardMissionInfo missionInfo)
	{
		string firstTip = missionInfo.MissionInfo_Ref.FirstTip;
		if (!firstTip.IsNullOrEmpty())
		{
			return firstTip;
		}
		BoardMissionTypeInfo missionLabel_Ref = missionInfo.MissionLabel_Ref;
		switch (missionLabel_Ref.MissionType)
		{
		case BoardMissionType.BATTLE:
		{
			if (DolocAPI.QueryMonsterDocument(missionInfo.Content.Args, out var document))
			{
				return DolocUtils.Format(missionLabel_Ref.MissionTipFormat, document.Title, missionInfo.Content.Count);
			}
			break;
		}
		case BoardMissionType.GATHER:
		{
			if (DolocAPI.QueryResourceDocument(missionInfo.Content.Args, out var document2))
			{
				return DolocUtils.Format(missionLabel_Ref.MissionTipFormat, document2.Title, missionInfo.Content.Count);
			}
			break;
		}
		case BoardMissionType.COLLECTION:
		{
			if (DolocAPI.QueryItemProto(missionInfo.Content.Args, out var proto))
			{
				return DolocUtils.Format(missionLabel_Ref.MissionTipFormat, proto.Title, missionInfo.Content.Count, sender);
			}
			break;
		}
		}
		return string.Empty;
	}
}
