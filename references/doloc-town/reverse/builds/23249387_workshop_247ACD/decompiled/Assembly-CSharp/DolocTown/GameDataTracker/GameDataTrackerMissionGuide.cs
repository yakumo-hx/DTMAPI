using System.Collections.Generic;
using DolocTown.Config.Settings;
using Newtonsoft.Json;

namespace DolocTown.GameDataTracker;

[JsonObject(MemberSerialization.OptIn)]
public class GameDataTrackerMissionGuide
{
	public class MissionListener : IMissionManagerComponent
	{
		public void OnMissionRemoved(IMission mission)
		{
		}

		public void OnMissionStart(IMission mission)
		{
			DolocAPI.archiveHandle.dataTracker.GdtMissionGuide.OnMissionReceived(mission.Id);
		}

		public void OnMissionCompleted(IMission mission)
		{
			DolocAPI.archiveHandle.dataTracker.GdtMissionGuide.OnMissionComplete(mission.Id);
		}

		public void OnMissionChanged(GameEventType type, GameEventArgs args, IMission mission)
		{
		}
	}

	[JsonProperty]
	private readonly Dictionary<string, int> receiveDayMap = new Dictionary<string, int>();

	public GameDataTrackerMissionGuide()
	{
	}

	[JsonConstructor]
	protected GameDataTrackerMissionGuide(Dictionary<string, int> receiveDayMap)
		: this()
	{
		this.receiveDayMap = receiveDayMap;
	}

	public void OnMissionReceived(string missionId)
	{
		if (DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR) && !receiveDayMap.ContainsKey(missionId))
		{
			receiveDayMap.Add(missionId, DolocAPI.archiveHandle.timeData.TotalDays);
		}
	}

	public void OnMissionComplete(string missionId)
	{
		if (DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR) && receiveDayMap.ContainsKey(missionId))
		{
			int receiveDay = receiveDayMap[missionId];
			DataUploader.TrackMissionData(missionId, receiveDay, DolocAPI.archiveHandle.timeData.TotalDays);
		}
	}
}
