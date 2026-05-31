using System.Collections.Generic;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[DebugObject]
public interface IMission
{
	[DebugInfo("任务ID")]
	string Id { get; }

	[DebugInfo("是否隐式")]
	bool IsImplicit { get; }

	[DebugInfo("是否完成")]
	bool IsComplete { get; }

	bool IsCompleteSafe { get; }

	bool IsCompleteLoadArchive { get; }

	[DebugInfo("完成状态")]
	string MissionStatus { get; }

	[DebugInfo("任务附属模块")]
	MissionAttachModule[] AttachModules { get; }

	bool isDeserializationValid { get; }

	bool IsInvalid { get; }

	[DebugInfo]
	string BriefStatus { get; }

	IEnumerable<MissionLog> MissionLogs { get; }

	[DebugInfo]
	bool HasTimeLimit { get; }

	[DebugInfo]
	int LeftTime { get; }

	[DebugInfo]
	string Title => BaseInfo?.Title ?? ("title\"" + Id + "\"");

	[DebugInfo]
	string Tip => NodeInfo?.Tip ?? ("tip\"" + Id + "\"");

	[DebugInfo]
	string Description => BaseInfo?.Description ?? ("description\"" + Id + "\"");

	[DebugInfo]
	string Sender => DolocAPI.GetNpcTitle(BaseInfo?.Sender, ignoreUnknown: true) ?? ("sender\"" + Id + "\"");

	MissionInfo BaseInfo { get; }

	MissionNodeInfo NodeInfo
	{
		get
		{
			if (BaseInfo == null)
			{
				return null;
			}
			BaseInfo.NodeInfos_Index.TryGetValue(Id, out var value);
			return value;
		}
	}

	[DebugInfo]
	List<Reward> Rewards { get; }

	[DebugInfo]
	bool HasReward { get; }

	void CashRewards();

	void AfterLoadData();

	bool SendMessage(GameEventType type, GameEventArgs args, out bool statusChanged);

	bool TryInvokeHistoryInSandBox(out string reason);

	bool UpdatePerHour();

	void ClearProgress();
}
