namespace DolocTown;

public interface IAnimalMilkingMachine : IAnimalInteractable
{
	bool IsFull { get; }

	bool IsAvailable { get; }

	void Produce(CountItem[] items);
}
