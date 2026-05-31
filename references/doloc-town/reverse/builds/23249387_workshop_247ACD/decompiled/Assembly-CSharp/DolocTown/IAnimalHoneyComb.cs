namespace DolocTown;

public interface IAnimalHoneyComb : IAnimalInteractable
{
	bool IsHoneyCombFull { get; }

	void ProduceHoney(CountItem[] items);
}
