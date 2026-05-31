using DolocTown.Config.Item;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemSeedMaternal : Item
{
	private ItemFunctionSeedMaternal func => base.proto.Function as ItemFunctionSeedMaternal;

	public ItemSeedMaternal(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemSeedMaternal(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override bool CanDispose()
	{
		if (!DolocAPI.archiveHandle.IsSeedNodeAvailableToUnlock(func.SeedItemId))
		{
			return DolocAPI.archiveHandle.IsSeedNodeUnlocked(func.SeedItemId);
		}
		return true;
	}

	protected override bool CanSell()
	{
		if (!DolocAPI.archiveHandle.IsSeedNodeAvailableToUnlock(func.SeedItemId))
		{
			return DolocAPI.archiveHandle.IsSeedNodeUnlocked(func.SeedItemId);
		}
		return true;
	}
}
