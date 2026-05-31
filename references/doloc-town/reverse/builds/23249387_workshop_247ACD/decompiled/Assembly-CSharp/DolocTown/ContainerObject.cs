using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public abstract class ContainerObject : InteractableObject, IContainer
{
	[SerializeField]
	protected CountItemListConfig itemList;

	public string title => textTitle.Text;

	public LinearInventory inventory { get; private set; }

	public abstract int totalCapacity { get; }

	public abstract int lineCapacity { get; }

	protected override void OnLoadData(Room room)
	{
		base.OnLoadData(room);
		if (base.archiveData.LoadInventory(base.guid, out var linearInventory) && linearInventory != null)
		{
			inventory = linearInventory;
			return;
		}
		Item[] items = itemList.GenerateItems();
		inventory = new LinearInventory(totalCapacity);
		inventory.Overwrite(items, shouldEmit: true);
		SaveInventory();
	}

	public abstract bool ContentFilter(Item item);

	protected sealed override void OnInteract()
	{
		base.OnInteract();
		OpenBox();
	}

	protected abstract void OpenBox();

	protected virtual void SaveInventory()
	{
		base.archiveData.SaveInventory(base.guid, inventory);
	}
}
