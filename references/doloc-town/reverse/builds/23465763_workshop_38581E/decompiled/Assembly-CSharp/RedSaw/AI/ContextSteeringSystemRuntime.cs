using DolocTown.GameData;
using UnityEngine;

namespace RedSaw.AI;

public class ContextSteeringSystemRuntime
{
	private readonly float timeToTargetReciprocal;

	private readonly float slowRadiusReciprocal;

	private readonly Transform transform;

	private readonly IMonsterMoverProtoCtxSteering _ctxSteering;

	private ContextSteering contextSteering;

	private Vector2 velocity;

	private Vector2 acceleration;

	public Vector2 bestDir;

	public bool FlipX => velocity.x > 0f;

	public int FlipXInt
	{
		get
		{
			if (!(velocity.x > 0f))
			{
				return 1;
			}
			return -1;
		}
	}

	public float xSpeedProcess => velocity.x / _ctxSteering.MaxSpeed;

	public float DangerRadius => _ctxSteering.DangerRadius;

	public ContextSteeringSystemRuntime(Transform transform, IMonsterMoverProtoCtxSteering ctxSteering)
	{
		this.transform = transform;
		_ctxSteering = ctxSteering;
		slowRadiusReciprocal = 1f / _ctxSteering.SlowRadius;
		timeToTargetReciprocal = 1f / _ctxSteering.TimeToTarget;
		contextSteering = new ContextSteering(_ctxSteering.DirCount);
		acceleration = Vector2.zero;
		velocity = Vector2.zero;
	}

	public void Flash(Vector2 pos)
	{
		transform.position = pos;
		contextSteering.Clear();
		velocity = Vector2.zero;
		acceleration = Vector2.zero;
	}

	public void Update(float dt, Vector2 targetPos, ContextSteeringObstacle[] obstacles)
	{
		Vector2 vector = targetPos - (Vector2)transform.position;
		float magnitude = vector.magnitude;
		Vector2 targetDir = vector / magnitude;
		Vector2 vector2 = contextSteering.CalcContextDir(targetDir, obstacles, _ctxSteering.Sensitive);
		float num = ((magnitude > _ctxSteering.SlowRadius) ? _ctxSteering.MaxSpeed : (_ctxSteering.MaxSpeed * (magnitude * slowRadiusReciprocal)));
		Vector2 vector3 = vector2 * num;
		acceleration = (vector3 - velocity) * timeToTargetReciprocal;
		acceleration = Vector2.ClampMagnitude(acceleration, _ctxSteering.MaxAcceleration);
		velocity += acceleration * dt;
		velocity = Vector2.ClampMagnitude(velocity, _ctxSteering.MaxSpeed);
		if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y))
		{
			acceleration = Vector2.zero;
			velocity = Vector2.zero;
		}
		transform.Translate(velocity * dt);
	}
}
