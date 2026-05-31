using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AnimalJump : AnimalTask
{
	private readonly Vector2Int dest;

	private bool hasStart;

	public AnimalJump(Vector2Int dest)
	{
		this.dest = dest;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (hasStart)
		{
			if (!animal.Jump())
			{
				return TaskStatus.Executing;
			}
			return TaskStatus.Success;
		}
		if (!animal.StartJump(dest))
		{
			return TaskStatus.Success;
		}
		hasStart = true;
		return TaskStatus.Executing;
	}
}
