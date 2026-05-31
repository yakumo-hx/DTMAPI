using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Room;

namespace DolocTown.UI;

public struct TeleportPointGroupData : IUIData
{
	public bool notEmpty { get; }

	public List<TeleportPointData> unlockedPoints { get; }

	public List<TeleportPointData> lockedPoints { get; }

	public TeleportPointGroupData(StationInfo[] allStations, StationInfo[] unlockedStations)
	{
		this = default(TeleportPointGroupData);
		notEmpty = true;
		if (unlockedStations == null)
		{
			unlockedStations = Array.Empty<StationInfo>();
		}
		unlockedPoints = new List<TeleportPointData>();
		lockedPoints = new List<TeleportPointData>();
		StationInfo[] array = unlockedStations;
		foreach (StationInfo stationProto in array)
		{
			TeleportPointData item = new TeleportPointData(stationProto, isUnlocked: true);
			if (item.notEmpty)
			{
				unlockedPoints.Add(item);
			}
		}
		array = allStations;
		foreach (StationInfo info in array)
		{
			if (!unlockedStations.Any((StationInfo x) => x.Id == info.Id))
			{
				TeleportPointData item2 = new TeleportPointData(info, isUnlocked: false);
				if (item2.notEmpty)
				{
					lockedPoints.Add(item2);
				}
			}
		}
	}
}
