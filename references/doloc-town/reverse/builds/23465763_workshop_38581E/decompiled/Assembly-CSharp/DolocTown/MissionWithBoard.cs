using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MissionWithBoard : IMission
{
	[JsonProperty]
	private string missionId;

	[JsonProperty]
	protected MissionContentHandle handle;

	[JsonProperty]
	private string rewardId;

	[JsonProperty]
	private int leftTime;

	private List<Reward> rewards;

	private BoardMissionInfo MissionProto => DolocConfig.Tables.TbBoardMission.GetOrDefault(missionId);

	public GameEventType GameEventType
	{
		get
		{
			Enum.TryParse<GameEventType>(MissionProto.Content.EventType, ignoreCase: true, out var result);
			return result;
		}
	}

	public string Id => missionId;

	public bool IsImplicit => false;

	public bool IsLoop => false;

	public bool isDeserializationValid => MissionProto != null;

	public bool IsInvalid => handle.IsInvalid;

	public bool IsComplete => handle.IsComplete;

	public bool IsCompleteLoadArchive => handle.IsCompleteLoadArchive;

	public bool IsCompleteSafe
	{
		get
		{
			try
			{
				return handle?.IsComplete ?? false;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public string MissionStatus => handle.MissionStatus;

	public string BriefStatus => handle.BriefStatus;

	public IEnumerable<MissionLog> MissionLogs => handle.MissionLogs;

	public int LeftTime => leftTime;

	public bool HasTimeLimit => MissionProto.TimeLimit > 0;

	public MissionInfo BaseInfo => DolocConfig.Tables.TbMission.GetOrDefault(missionId);

	public bool HasReward
	{
		get
		{
			if (Rewards != null)
			{
				return Rewards.Count != 0;
			}
			return false;
		}
	}

	public MissionAttachModule[] AttachModules => Array.Empty<MissionAttachModule>();

	public string Sender
	{
		get
		{
			DolocAPI.QueryNpc(BaseInfo.Sender, out var npc);
			return npc.OriginTitle;
		}
	}

	public string Tip
	{
		get
		{
			if (!BaseInfo.FirstTip.IsNullOrEmpty())
			{
				return BaseInfo.FirstTip;
			}
			BoardMissionTypeInfo missionLabel_Ref = MissionProto.MissionLabel_Ref;
			switch (missionLabel_Ref.MissionType)
			{
			case BoardMissionType.BATTLE:
			{
				if (DolocAPI.QueryMonsterDocument(MissionProto.Content.Args, out var document))
				{
					return DolocUtils.Format(missionLabel_Ref.MissionTipFormat, document.Title, MissionProto.Content.Count);
				}
				break;
			}
			case BoardMissionType.GATHER:
			{
				if (DolocAPI.QueryResourceDocument(MissionProto.Content.Args, out var document2))
				{
					return DolocUtils.Format(missionLabel_Ref.MissionTipFormat, document2.Title, MissionProto.Content.Count);
				}
				break;
			}
			case BoardMissionType.COLLECTION:
			{
				if (DolocAPI.QueryItemProto(MissionProto.Content.Args, out var proto))
				{
					return DolocUtils.Format(missionLabel_Ref.MissionTipFormat, proto.Title, MissionProto.Content.Count, Sender);
				}
				break;
			}
			}
			return string.Empty;
		}
	}

	public List<Reward> Rewards
	{
		get
		{
			if (rewards != null)
			{
				return rewards;
			}
			RewardPoolInfo value;
			RewardProto[] protos = (DolocConfig.Tables.TbRewardPool.DataMap.TryGetValue(rewardId, out value) ? value.Rewards : MissionProto.Rewards);
			rewards = new List<Reward>(DolocAPI.CreateRewards(protos));
			return rewards;
		}
	}

	public MissionWithBoard(string missionId, MissionContentHandle handle, string rewardId, int leftTime)
	{
		this.missionId = missionId;
		this.handle = handle;
		this.rewardId = rewardId;
		this.leftTime = leftTime;
	}

	public virtual void AfterLoadData()
	{
	}

	public bool SendMessage(GameEventType type, GameEventArgs args, out bool statusChanged)
	{
		return handle.SendMessage(type, args, out statusChanged);
	}

	public bool TryInvokeHistoryInSandBox(out string reason)
	{
		reason = "不支持对看板任务进行历史重建";
		return false;
	}

	public void CashRewards()
	{
		DolocAPI.AddBattleExp(DolocAPI.archiveHandle.GetBoardMissionExp(MissionProto.Level));
		DolocAPI.AddTargetNpcLikingValue(BaseInfo.Sender, MissionProto.Favorability);
		if (!HasReward)
		{
			return;
		}
		if (MissionProto.ShouldSendEmail)
		{
			DolocAPI.ShowMessageBoxSmall(string.Format(DolocConfig.StaticTexts.MissionCompleteSendEmail, BaseInfo.Title));
			DolocAPI.archiveHandle.farmData.emailManager.SendItemsAsEmail(Rewards, "board_mission_template");
			return;
		}
		foreach (Reward reward in Rewards)
		{
			reward.CashRewardOverflowAsEmail(BaseInfo?.Title);
		}
	}

	public bool UpdatePerHour()
	{
		if (!HasTimeLimit)
		{
			return false;
		}
		if (--leftTime > 0)
		{
			return false;
		}
		if (GameEventType == GameEventType.COMPLETE_DIALOGUE)
		{
			DolocAPI.RemoveDialogueNode(missionId);
		}
		return true;
	}

	public void ClearProgress()
	{
		handle.ClearProgress();
	}
}
