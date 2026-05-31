using UnityEngine;

namespace RedSaw.AI;

public class ContextSteeringSystem : MonoBehaviour
{
	[Tooltip("调节该速度可使无人机整体的运动速度发生变化")]
	[SerializeField]
	public float maxSpeed;

	[Tooltip("调节该速度可使无人机整体的响应速度发生变化")]
	[SerializeField]
	public float maxAcceleration;

	[Tooltip("该值越大则无人机运动的轨迹越模糊")]
	[SerializeField]
	public float targetRadius;

	[SerializeField]
	public float slowRadius;

	[Tooltip("该值越小则无人机响应的速度越快,行动越机械")]
	[SerializeField]
	[Range(0.1f, 5f)]
	public float timeToTarget;

	[Tooltip("该值越大则无人机的障碍规避越细节,但是整体性能会下降")]
	[SerializeField]
	[Range(3f, 24f)]
	public int dirCount;

	[SerializeField]
	[Range(0.1f, 10f)]
	public float dangerRadius;

	[SerializeField]
	[Range(0f, 5f)]
	public float sensitive;

	private ContextSteering contextSteering;

	private Vector2 velocity;

	private Vector2 acceleration;

	private bool isInitialized;

	public bool moveDirection => velocity.x > 0f;

	public float xSpeedProcess => velocity.x / maxSpeed;

	public void Init()
	{
		isInitialized = true;
		contextSteering = new ContextSteering(dirCount);
		acceleration = Vector2.zero;
		velocity = Vector2.zero;
	}

	public void UpdateStatus(Vector2 targetPos, ContextSteeringObstacle[] obstacles)
	{
		Vector2 vector = targetPos - (Vector2)base.transform.position;
		float magnitude = vector.magnitude;
		Vector2 targetDir = vector / magnitude;
		Vector2 vector2 = contextSteering.CalcContextDir(targetDir, obstacles, sensitive);
		float num = ((magnitude > slowRadius) ? maxSpeed : (maxSpeed * (magnitude / slowRadius)));
		Vector2 vector3 = vector2 * num;
		acceleration = (vector3 - velocity) / timeToTarget;
		acceleration = Vector2.ClampMagnitude(acceleration, maxAcceleration);
	}

	public void OnFixedUpdate(float deltaTime)
	{
		velocity += acceleration * deltaTime;
		velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
		base.transform.Translate(velocity * deltaTime);
	}
}
