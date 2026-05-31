using UnityEngine;

namespace DolocTown;

public class AnimalMover
{
	private Vector2Int start;

	private Vector2Int end;

	private float totalDistance;

	private float leftDistance;

	private float walkSpeed;

	private int moveDuration;

	public float Velocity { get; private set; }

	public Vector2 positionWS { get; private set; }

	public void SetMoveInfo(Vector2 positionWS, Vector2Int start, Vector2Int end, float walkSpeed)
	{
		this.positionWS = positionWS;
		this.start = start;
		this.end = end;
		int num = end.x - start.x;
		totalDistance = (float)Mathf.Abs(num) * 1.5f;
		moveDuration = Mathf.FloorToInt(totalDistance / walkSpeed) + 1;
		this.walkSpeed = totalDistance / (float)moveDuration;
		Velocity = Mathf.Sign(num) * this.walkSpeed;
		leftDistance = totalDistance;
	}

	public bool Move(out Vector2Int currentPosition)
	{
		moveDuration--;
		leftDistance -= walkSpeed;
		positionWS += new Vector2(Velocity, 0f);
		if (moveDuration <= 0)
		{
			currentPosition = end;
			return true;
		}
		float num = 1f - leftDistance / totalDistance;
		currentPosition = new Vector2Int(Mathf.RoundToInt((float)start.x + (float)(end.x - start.x) * num), start.y);
		return false;
	}
}
