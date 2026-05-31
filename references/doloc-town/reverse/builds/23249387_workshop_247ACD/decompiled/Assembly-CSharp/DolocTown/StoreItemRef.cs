namespace DolocTown;

public struct StoreItemRef
{
	public string itemName;

	public bool isBuyback;

	public StoreItemRef(string itemName, bool isBuyback)
	{
		this.itemName = itemName;
		this.isBuyback = isBuyback;
	}
}
