namespace DolocTown;

public interface IStore
{
	void Refresh();

	bool UnlockStoreItem(string itemName);

	void RevertUnlockStoreItem(string itemName);

	int GetSoldCount(string itemName);
}
