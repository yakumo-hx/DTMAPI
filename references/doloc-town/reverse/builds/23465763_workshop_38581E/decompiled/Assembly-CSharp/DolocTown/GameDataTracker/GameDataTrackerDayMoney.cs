using System.Collections.Generic;
using DolocTown.Config.Settings;
using Newtonsoft.Json;

namespace DolocTown.GameDataTracker;

[JsonObject(MemberSerialization.OptIn)]
public class GameDataTrackerDayMoney
{
	[JsonProperty("days")]
	private readonly HashSet<int> _recordedDays = new HashSet<int>();

	public GameDataTrackerDayMoney()
	{
	}

	[JsonConstructor]
	public GameDataTrackerDayMoney(HashSet<int> days)
	{
		_recordedDays = days;
	}

	public void Record()
	{
		if (DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR))
		{
			int totalDays = DolocAPI.archiveHandle.timeData.TotalDays;
			if (_recordedDays.Add(totalDays))
			{
				int currentMoney = DolocAPI.archiveHandle.CurrentMoney;
				int accumulation = DolocAPI.archiveHandle.farmData.eventRecorderManager.GetAccumulation(GameEventType.MAKE_MONEY);
				DataUploader.TraceDayMoney(totalDays, currentMoney, accumulation);
			}
		}
	}
}
