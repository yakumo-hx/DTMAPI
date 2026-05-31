using System.Collections.Generic;

namespace DolocTown;

public interface IContainer
{
	string title { get; }

	LinearInventory inventory { get; }

	int totalCapacity { get; }

	int lineCapacity { get; }

	bool ContentFilter(Item content);

	void OverwriteInventory(IEnumerable<CountItem> countItems)
	{
		inventory.Overwrite(countItems.GenerateItems(), shouldEmit: true);
	}

	void OverwriteInventory(Item[] items)
	{
		inventory.Overwrite(items, shouldEmit: true);
	}
}
