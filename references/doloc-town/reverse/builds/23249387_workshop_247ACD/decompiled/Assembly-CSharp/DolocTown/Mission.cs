using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class Mission : IMission
{
	[JsonProperty]
	[DebugInfo]
	private string chainId;

	[JsonProperty]
	[DebugInfo]
	private string missionId;

	[JsonProperty]
	[DebugInfo]
	protected MissionContentHandle contentHandle;

	[JsonProperty]
	[DebugInfo]
	private bool isImplicit;

	[JsonProperty]
	[DebugInfo]
	private MissionAttachModule[] attachModules;

	private bool shouldSendEmail;

	public string ChainId => chainId;

	public string Id => missionId;

	public virtual bool IsImplicit => isImplicit;

	public bool isDeserializationValid => true;

	public bool IsInvalid
	{
		get
		{
			if (contentHandle.IsInvalid)
			{
				return true;
			}
			if (!DolocAPI.assets.missionChains.QueryData(chainId, out var data))
			{
				return true;
			}
			if (!data.QueryMissionNode(missionId, out var _))
			{
				return true;
			}
			return false;
		}
	}

	public bool IsComplete => contentHandle.IsComplete;

	public bool IsCompleteLoadArchive => contentHandle.IsCompleteLoadArchive;

	public bool IsCompleteSafe
	{
		get
		{
			try
			{
				return contentHandle?.IsComplete ?? false;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public string MissionStatus => contentHandle.MissionStatus;

	public string BriefStatus => contentHandle.BriefStatus;

	public IEnumerable<MissionLog> MissionLogs => contentHandle.MissionLogs;

	public int LeftTime => -1;

	public bool HasTimeLimit => false;

	public string Sender => string.Empty;

	public MissionInfo BaseInfo => DolocConfig.Tables.TbMission.GetOrDefault(chainId);

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

	public MissionAttachModule[] AttachModules => attachModules;

	public List<Reward> Rewards
	{
		get
		{
			if (!DolocAPI.assets.missionChains.QueryData(chainId, out var data))
			{
				return null;
			}
			if (!data.QueryMissionRewards(missionId, out var rewards, out shouldSendEmail))
			{
				return null;
			}
			return rewards;
		}
	}

	public Mission(string chainId, string missionId, MissionContentHandle contentHandle, bool isImplicit = false, MissionAttachModule[] attachModules = null)
	{
		this.chainId = chainId;
		this.missionId = missionId;
		this.contentHandle = contentHandle;
		this.isImplicit = isImplicit;
		this.attachModules = attachModules;
	}

	public virtual void AfterLoadData()
	{
		if (!IsInvalid && DolocAPI.assets.missionChains.QueryData(chainId, out var data) && data.QueryMissionNode(missionId, out var node))
		{
			isImplicit = node.IsImplicit;
			attachModules = node.AttachModules;
		}
	}

	public virtual bool SendMessage(GameEventType type, GameEventArgs args, out bool statusChanged)
	{
		return contentHandle.SendMessage(type, args, out statusChanged);
	}

	public virtual bool TryInvokeHistoryInSandBox(out string reason)
	{
		return contentHandle.TryInvokeHistoryInSandBox(out reason);
	}

	public virtual void CashRewards()
	{
		List<Reward> rewards = Rewards;
		if (rewards == null || rewards.Count == 0)
		{
			return;
		}
		if (shouldSendEmail)
		{
			DolocAPI.archiveHandle.farmData.emailManager.SendItemsAsEmail(rewards, "send_item_template");
			return;
		}
		foreach (Reward item in rewards)
		{
			item.CashRewardOverflowAsEmail(BaseInfo?.Title);
		}
	}

	public bool UpdatePerHour()
	{
		return false;
	}

	public void ClearProgress()
	{
		contentHandle.ClearProgress();
	}
}
