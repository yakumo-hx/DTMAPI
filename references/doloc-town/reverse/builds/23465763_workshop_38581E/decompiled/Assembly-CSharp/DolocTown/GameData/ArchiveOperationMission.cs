using Cysharp.Threading.Tasks;
using DolocTown.NodeCanvas;
using UnityEngine;

namespace DolocTown.GameData;

public static class ArchiveOperationMission
{
	public static void AppendMissionLog(this ArchiveDataHandle handle, string missionLog)
	{
	}

	public static bool StartChainMission(this ArchiveDataHandle handle, string chainId, string missionId, MissionContent content, bool isImplicit = false, string decoratorId = null, MissionAttachModule[] modules = null)
	{
		if (chainId.IsNullOrEmpty() || content == null)
		{
			return false;
		}
		MissionContentHandle contentHandle = content.CreateHandle();
		IMission mission = (decoratorId.IsNullOrEmpty() ? new Mission(chainId, missionId, contentHandle, isImplicit, modules) : new MissionDecorator(chainId, missionId, contentHandle, decoratorId, modules));
		handle._StartMission(contentHandle, mission);
		return true;
	}

	public static bool StartMission(this ArchiveDataHandle handle, string missionId, MissionContent content, string rewardId, int leftTime = -1)
	{
		if (missionId.IsNullOrEmpty())
		{
			return false;
		}
		if (content == null)
		{
			return false;
		}
		MissionContentHandle missionContentHandle = content.CreateHandle();
		MissionWithBoard missionWithBoard = new MissionWithBoard(missionId, missionContentHandle, rewardId, leftTime);
		handle._StartMission(missionContentHandle, missionWithBoard);
		if (missionWithBoard.GameEventType == GameEventType.COMPLETE_DIALOGUE)
		{
			DolocAPI.AddDialogueNode(missionId);
		}
		return true;
	}

	private static void _StartMission(this ArchiveDataHandle handle, MissionContentHandle contentHandle, IMission mission)
	{
		if (contentHandle.IsCompleteBeforeInit())
		{
			UniTask.DelayFrame(1).ContinueWith(delegate
			{
				handle.farmData.missionManager.CompleteMission(mission);
			}).Forget();
		}
		else
		{
			handle.farmData.missionManager.StartMission(mission);
		}
	}

	public static bool ContainsCustomGameEvent(this ArchiveDataHandle handle, string name)
	{
		return handle.farmData.eventRecorderManager.GetCount(GameEventType.CUSTOM, name) > 0;
	}

	public static bool QueryMission(this ArchiveDataHandle handle, string missionId, out IMission mission)
	{
		return handle.farmData.missionManager.QueryMission(missionId, out mission);
	}

	public static IMission[] GetMissions(this ArchiveDataHandle handle)
	{
		return handle.farmData.missionManager.TotalExplicitMissions;
	}

	public static bool RemoveMission(this ArchiveDataHandle handle, string id)
	{
		return handle.farmData.missionManager.RemoveMission(id);
	}

	public static bool StartMissionChain(this ArchiveDataHandle handle, MissionGraph missionGraph)
	{
		if (missionGraph == null)
		{
			return false;
		}
		return handle.farmData.missionChainManager._StartMissionChain(missionGraph);
	}

	public static void StopMissionChain(this ArchiveDataHandle handle, string missionChainId)
	{
		if (!missionChainId.IsNullOrEmpty())
		{
			handle.farmData.missionChainManager.StopMissionChain(missionChainId);
		}
	}

	public static void RemoveMissionCompleteRecord(this ArchiveDataHandle handle, string missionId)
	{
		if (!missionId.IsNullOrEmpty())
		{
			handle.farmData.missionManager.RemoveMissionCompleteRecord(missionId);
		}
	}

	public static void ClearMissionChainRecords(this ArchiveDataHandle handle, MissionGraph missionGraph)
	{
		foreach (string allMissionId in missionGraph.AllMissionIds)
		{
			handle.RemoveMissionCompleteRecord(allMissionId);
		}
	}

	public static bool StartFactionMission(this ArchiveDataHandle handle, string missionId)
	{
		return handle.cityData.factionMissionManager.AddFactionMission(missionId);
	}

	public static FactionMission[] GetTotalFactionMissions(this ArchiveDataHandle handle)
	{
		return handle.cityData.factionMissionManager.TotalMissions;
	}

	public static void StartBoardMission(this ArchiveDataHandle handle, string missionId)
	{
		handle.cityData.boardMissionManager.StartBoardMission(missionId);
	}

	public static bool AcceptBoardMission(this ArchiveDataHandle handle, string missionId)
	{
		return handle.cityData.boardMissionManager.AcceptBoardMission(missionId);
	}

	public static bool IssueBoardMission(this ArchiveDataHandle handle, string missionId)
	{
		return handle.cityData.boardMissionManager.IssueMission(missionId);
	}

	public static int GetBoardMissionExp(this ArchiveDataHandle handle, string missionLv)
	{
		return handle.cityData.boardMissionManager.GetMissionExp(missionLv);
	}

	public static void AddEventDecorator(this ArchiveDataHandle handle, string id)
	{
		handle.farmData.missionManager.ManualAddDecorator(id);
	}

	public static void RemoveEventDecorator(this ArchiveDataHandle handle, string id)
	{
		handle.farmData.missionManager.ManualRemoveDecorator(id);
	}

	public static bool IsEventDecoratorComplete(this ArchiveDataHandle handle, string id)
	{
		return handle.farmData.missionManager.CheckDecorator(id);
	}

	public static string[] GetAllCompletedEventDecorator(this ArchiveDataHandle handle)
	{
		return handle.farmData.missionManager.FinishDecorators;
	}

	public static void InvokeFirstComeoutAfterSleep(this ArchiveDataHandle handle)
	{
		if (!handle.farmData.agentData.hasComeoutAfterSleep)
		{
			handle.farmData.agentData.hasComeoutAfterSleep = true;
			DolocAPI.Broadcast(GameEventType.FIRST_COMEOUT_AFTER_SLEEP);
			Debug.Log("玩家睡觉之后第一次出门了..");
		}
	}

	public static int GetGameEventTypeIntAccumulation(this ArchiveDataHandle handle, GameEventType eventType)
	{
		return handle.farmData.eventRecorderManager.GetAccumulation(eventType);
	}

	public static void TraceBackMoneyMade(this ArchiveDataHandle handle, int value)
	{
		((GameEventRecorderInt)handle.farmData.eventRecorderManager.GetRecorder(GameEventType.MAKE_MONEY)).UnRecord(value);
	}
}
