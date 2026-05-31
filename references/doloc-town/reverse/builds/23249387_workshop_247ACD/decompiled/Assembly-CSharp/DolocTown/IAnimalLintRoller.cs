namespace DolocTown;

public interface IAnimalLintRoller : IAnimalInteractable
{
	bool IsFull { get; }

	void Produce(CountItem[] items);
}
