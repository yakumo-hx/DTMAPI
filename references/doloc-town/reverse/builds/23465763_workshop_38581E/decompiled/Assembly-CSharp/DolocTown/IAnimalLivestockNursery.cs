namespace DolocTown;

public interface IAnimalLivestockNursery : IAnimalInteractable
{
	bool IsLivestockNurseryFree { get; }

	void StartBreed(int breedDuration);
}
