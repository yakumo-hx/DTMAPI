using System.Collections.Generic;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AutomateTaskMoveInt : AutomateTaskMoveBase
{
	private readonly Vector2Int target;

	private readonly int pathStep;

	private Queue<Vector2Int> path;

	private Room room;

	public AutomateTaskMoveInt(Vector2Int target, int pathStep, LinearTaskBreaker breaker = null)
		: base(breaker)
	{
		this.target = target;
		this.pathStep = pathStep;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (path.Count > 0)
		{
			base.Bot.Move(path.Dequeue());
			return TaskStatus.Executing;
		}
		return TaskStatus.Success;
	}

	public override void OnBegin()
	{
		room = base.Bot.CurrentRoom;
		Vector2Int[] collection = AutomateUtils.VoidPathStep(base.Bot.PositionCell, target, pathStep);
		base.Bot.Renderer?.DrawPath(collection, room.RoomPosition);
		path = new Queue<Vector2Int>(collection);
	}
}
