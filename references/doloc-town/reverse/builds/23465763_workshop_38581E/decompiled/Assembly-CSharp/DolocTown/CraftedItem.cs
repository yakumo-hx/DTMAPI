namespace DolocTown;

public class CraftedItem
{
	public CountItem targetItem;

	public CountItem[] costItems;

	public CraftedItem(CountItem targetItem, CountItem[] costItems)
	{
		this.targetItem = targetItem;
		this.costItems = costItems;
	}
}
