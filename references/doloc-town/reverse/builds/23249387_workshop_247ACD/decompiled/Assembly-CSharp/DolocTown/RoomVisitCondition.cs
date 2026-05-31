using System;
using UnityEngine;

namespace DolocTown;

[Serializable]
public class RoomVisitCondition : ICondition
{
	[SerializeField]
	private string roomId;

	public bool IsConditionMet(bool reverseCondition)
	{
		if (roomId.IsNullOrEmpty())
		{
			return true;
		}
		return DolocAPI.archiveHandle.farmData.mapManager.IsRoomVisited(roomId) != reverseCondition;
	}

	public override string ToString()
	{
		return "到达过房间<" + roomId + ">";
	}
}
