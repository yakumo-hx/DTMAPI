using UnityEngine;

namespace DolocTown;

public class TransportOverrideInfo
{
	public string roomId;

	public Vector2 position;

	public TransportOverrideInfo(string roomId, Vector2 position)
	{
		this.roomId = roomId;
		this.position = position;
	}
}
