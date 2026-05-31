using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AnimalMove : AnimalTask
{
	private readonly Vector2Int dest;

	private bool hasStart;

	private bool hasReachTarget;

	public AnimalMove(Vector2Int dest)
	{
		this.dest = dest;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (!hasStart)
		{
			hasStart = true;
			animal.StartMove(dest);
			return TaskStatus.Executing;
		}
		if (hasReachTarget)
		{
			animal.StopMove();
			return TaskStatus.Success;
		}
		if (animal.Move())
		{
			hasReachTarget = true;
		}
		return TaskStatus.Executing;
	}

	public override void OnBreak()
	{
		base.OnBreak();
		if (hasStart && !hasReachTarget)
		{
			animal.StopMove();
		}
	}
}
