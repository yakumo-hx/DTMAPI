namespace DolocTown;

public struct CountItemContainer
{
	public Case container;

	public CountItem item;

	public string ItemName => item.itemName;

	public int ItemCount => item.itemCount;

	public CountItemContainer(Case container, CountItem item)
	{
		this.container = container;
		this.item = item;
	}
}
