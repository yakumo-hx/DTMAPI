using UnityEngine;

namespace RedSaw.AI;

public struct ContextSteeringObstacle
{
	public Vector2 positionWS;

	public Vector2 errorVec;

	public float distance;

	public ContextSteeringObstacle(Vector2 positionWS, Vector2 errorVec, float distance)
	{
		this.positionWS = positionWS;
		this.errorVec = errorVec;
		this.distance = distance;
	}
}
