namespace DolocTown;

public interface IGatherableEquipment
{
	Room CurrentRoom { get; }

	bool IsGatherable { get; }

	Item[] Gather();
}
