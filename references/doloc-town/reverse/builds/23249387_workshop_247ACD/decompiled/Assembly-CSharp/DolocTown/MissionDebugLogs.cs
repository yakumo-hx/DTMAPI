using DolocTown.GameData;

namespace DolocTown;

public class MissionDebugLogs : IMissionManagerComponent
{
	public void OnMissionStart(IMission mission)
	{
		DolocAPI.archiveHandle.AppendMissionLog("任务\"" + mission.Id + "\"启动");
	}

	public void OnMissionCompleted(IMission mission)
	{
		DolocAPI.archiveHandle.AppendMissionLog("任务\"" + mission.Id + "\"完成");
	}

	public void OnMissionRemoved(IMission mission)
	{
		DolocAPI.archiveHandle.AppendMissionLog("任务\"" + mission.Id + "\"被移除");
	}

	public void OnMissionChanged(GameEventType type, GameEventArgs args, IMission mission)
	{
		string arg = ((args is GameEventArgsString gameEventArgsString) ? gameEventArgsString._ToString() : args.ToString());
		DolocAPI.archiveHandle.AppendMissionLog($"因消息\"{type}({arg})\"致使任务\"{mission.Id}\"进度发生变化");
		DolocAPI.archiveHandle.AppendMissionLog("任务\"" + mission.Id + "\"当前进度: " + mission.MissionStatus);
	}
}
