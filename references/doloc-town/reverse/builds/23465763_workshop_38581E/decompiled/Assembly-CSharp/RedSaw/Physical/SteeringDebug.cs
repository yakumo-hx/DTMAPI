using UnityEngine;

namespace RedSaw.Physical;

public struct SteeringDebug
{
	private readonly ISteeringParamsProvider paramsProvider;

	private Vector2 velocity;

	public SteeringDebug(ISteeringParamsProvider provider)
	{
		paramsProvider = provider;
		velocity = Vector2.zero;
	}

	public void SetVelocity(Vector2 velocity)
	{
		this.velocity = velocity;
	}

	public Vector2 Move(Vector2 pos, Vector2 target, float dt)
	{
		Vector2 errorVec = target - pos;
		Vector2 vector = CalcParkingAcc(velocity, errorVec, paramsProvider.MaxSpeed);
		velocity += vector * dt;
		return pos + velocity * dt;
	}

	private readonly Vector2 CalcParkingAcc(Vector2 velocity, Vector2 errorVec, float maxSpeed)
	{
		float magnitude = errorVec.magnitude;
		float num = ((magnitude > paramsProvider.ParkingRadius) ? maxSpeed : (maxSpeed * (magnitude / paramsProvider.ParkingRadius)));
		return (errorVec.normalized * num - velocity) / paramsProvider.ResponseDuration;
	}
}
