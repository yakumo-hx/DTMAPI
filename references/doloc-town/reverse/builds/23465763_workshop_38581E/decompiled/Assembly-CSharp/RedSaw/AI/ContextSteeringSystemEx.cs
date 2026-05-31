using UnityEngine;

namespace RedSaw.AI;

public class ContextSteeringSystemEx
{
	private readonly float maxSpeed;

	private readonly float maxAcceleration;

	private readonly float slowRadius;

	private readonly float dangerRadius;

	private readonly float sensitive;

	private readonly float timeToTargetReciprocal;

	private readonly float slowRadiusReciprocal;

	private readonly float targetRadius;

	private readonly int dirCount;

	private readonly Transform transform;

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

	public float xSpeedProcess => velocity.x / maxSpeed;

	public float DangerRadius => dangerRadius;

	public ContextSteeringSystemEx(Transform transform, float maxSpeed, float maxAcceleration, float targetRadius, float slowRadius, float timeToTarget, int dirCount, float dangerRadius, float sensitive)
	{
		this.transform = transform;
		this.maxSpeed = maxSpeed;
		this.maxAcceleration = maxAcceleration;
		this.targetRadius = targetRadius;
		this.slowRadius = slowRadius;
		this.dangerRadius = dangerRadius;
		this.sensitive = sensitive;
		this.dirCount = dirCount;
		slowRadiusReciprocal = 1f / slowRadius;
		timeToTargetReciprocal = 1f / timeToTarget;
		contextSteering = new ContextSteering(dirCount);
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
		Vector2 vector2 = contextSteering.CalcContextDir(targetDir, obstacles, sensitive);
		float num = ((magnitude > slowRadius) ? maxSpeed : (maxSpeed * (magnitude * slowRadiusReciprocal)));
		Vector2 vector3 = vector2 * num;
		acceleration = (vector3 - velocity) * timeToTargetReciprocal;
		acceleration = Vector2.ClampMagnitude(acceleration, maxAcceleration);
		velocity += acceleration * dt;
		velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
		if (float.IsNaN(velocity.x) || float.IsNaN(velocity.y))
		{
			acceleration = Vector2.zero;
			velocity = Vector2.zero;
		}
		transform.Translate(velocity * dt);
	}
}
