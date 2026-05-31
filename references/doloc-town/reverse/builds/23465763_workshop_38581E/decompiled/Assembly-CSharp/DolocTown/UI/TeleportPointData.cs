using DolocTown.Config.Room;
using UnityEngine;

namespace DolocTown.UI;

public struct TeleportPointData : IUIData
{
	public bool notEmpty { get; }

	public string id { get; }

	public Vector2Int mapPosition { get; }

	public bool isUnlocked { get; }

	public TeleportPointData(StationInfo stationProto, bool isUnlocked)
	{
		this = default(TeleportPointData);
		if (stationProto != null)
		{
			notEmpty = true;
			id = stationProto.Id;
			mapPosition = stationProto.MapPosition;
			this.isUnlocked = isUnlocked;
		}
	}
}
