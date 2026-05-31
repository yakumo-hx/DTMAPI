namespace DolocTown;

public interface IAnimalToilet : IAnimalInteractable
{
	bool IsToiletFull { get; }

	void Excrete();
}
