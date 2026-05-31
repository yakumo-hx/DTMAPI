using UnityEngine;

namespace RedSaw.Physical;

public struct Steering
{
	public readonly float parkingRadius;

	public readonly float responseDuration;

	public readonly float maxSpeed;

	public readonly float parkingRadiusReciprocal;

	public readonly float responseDurationReciprocal;

	private Vector2 velocity;

	public Steering(float parkingRadius, float responseDuration, float maxSpeed)
	{
		this.parkingRadius = parkingRadius;
		this.responseDuration = responseDuration;
		this.maxSpeed = maxSpeed;
		parkingRadiusReciprocal = 1f / parkingRadius;
		responseDurationReciprocal = 1f / responseDuration;
		velocity = Vector2.zero;
	}

	public void SetVelocity(Vector2 velocity)
	{
		this.velocity = velocity;
	}

	public Vector2 Move(Vector2 pos, Vector2 target, float dt)
	{
		Vector2 errorVec = target - pos;
		Vector2 vector = CalcParkingAcc(velocity, errorVec, maxSpeed);
		velocity += vector * dt;
		return pos + velocity * dt;
	}

	private readonly Vector2 CalcParkingAcc(Vector2 velocity, Vector2 errorVec, float maxSpeed)
	{
		float magnitude = errorVec.magnitude;
		float num = ((magnitude > parkingRadius) ? maxSpeed : (maxSpeed * (magnitude * parkingRadiusReciprocal)));
		return (errorVec.normalized * num - velocity) * responseDurationReciprocal;
	}
}
