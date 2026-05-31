namespace DolocTown.GameData;

public struct SpawnedItemHoldPlace : ISpawnedItem
{
	public string SpawnId { get; }

	public int Volume { get; }

	public SpawnedItemHoldPlace(int volume)
	{
		SpawnId = "";
		Volume = volume;
	}
}
