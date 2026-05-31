using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class NpcTaskMove : NpcTask
{
	private readonly float destination;

	private float moveSpeed;

	private float totalDistance;

	public float velocity { get; private set; }

	public NpcTaskMove(float dest)
	{
		destination = dest;
	}

	public override void OnBegin()
	{
		moveSpeed = base._npc.proto.WalkSpeed;
		totalDistance = Mathf.Abs(destination - base._npc.positionWS.x);
		velocity = ((destination - base._npc.positionWS.x > 0f) ? moveSpeed : (0f - moveSpeed));
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (totalDistance > moveSpeed)
		{
			base._npc.Move(velocity);
			totalDistance -= moveSpeed;
			return TaskStatus.Executing;
		}
		if (totalDistance > 0f)
		{
			base._npc.Move(Mathf.Sign(velocity) * totalDistance);
			totalDistance = 0f;
			return TaskStatus.Executing;
		}
		return TaskStatus.Success;
	}

	public override void OnSuccess()
	{
		base._npc.StopMove();
	}

	public override string ToString()
	{
		return $"移动至:{destination}";
	}
}
