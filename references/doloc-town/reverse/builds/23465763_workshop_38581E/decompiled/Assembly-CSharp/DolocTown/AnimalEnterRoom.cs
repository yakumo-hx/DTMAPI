using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AnimalEnterRoom : AnimalTask
{
	private readonly Room room;

	public AnimalEnterRoom(Room room)
	{
		this.room = room;
	}

	public override TaskStatus OnExecute(float dt)
	{
		animal.EnterRoom(room);
		return TaskStatus.Success;
	}
}
