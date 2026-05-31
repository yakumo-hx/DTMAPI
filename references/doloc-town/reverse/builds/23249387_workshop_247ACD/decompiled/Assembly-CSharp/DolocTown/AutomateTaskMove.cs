using System.Collections.Generic;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AutomateTaskMove : AutomateTaskMoveBase
{
	private readonly Vector2 target;

	private readonly Room futureRoom;

	private Queue<Vector2Int> path;

	private Room room;

	public AutomateTaskMove(Vector2 target, Room futureRoom = null, LinearTaskBreaker breaker = null)
		: base(breaker)
	{
		this.target = target;
		this.futureRoom = futureRoom;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (path.Count <= 0)
		{
			return TaskStatus.Success;
		}
		base.Bot.Move(path.Dequeue());
		return TaskStatus.Executing;
	}

	public override void OnBegin()
	{
		room = futureRoom ?? base.Bot.CurrentRoom;
		Vector2Int to = room.Geometry.CalcCellPosition(target);
		Vector2Int[] collection = AutomateUtils.VoidPathStep(base.Bot.PositionCell, to, base.Bot.proto.speed);
		base.Bot.Renderer?.DrawPath(collection, room.RoomPosition);
		path = new Queue<Vector2Int>(collection);
	}
}
