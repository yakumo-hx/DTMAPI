using UnityEngine;

namespace DolocTown;

public class OnewayColliderProto
{
	public Vector2 position;

	public float worldWidth;

	public OnewayColliderProto(Vector2 position, float worldWidth)
	{
		this.position = position;
		this.worldWidth = worldWidth;
	}
}
