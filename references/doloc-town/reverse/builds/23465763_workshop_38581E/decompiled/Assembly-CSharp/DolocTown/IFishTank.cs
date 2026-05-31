using DolocTown.Config.Item;

namespace DolocTown;

public interface IFishTank : IContainer
{
	FarmFishTank tank { get; }

	int Energy { get; }

	int EnergyCapacity { get; }

	float EnergyPercent { get; }

	bool CanAddFeeds { get; }

	Equipment FishTankEquipmentEntity { get; }

	void AddFeeds(ItemInfo proto);
}
