using DolocTown.Config.Fishing;

namespace DolocTown;

public class FarmFish
{
	public readonly FarmFishInfo proto;

	public int count;

	public int TotalEnergyCost => proto.EnergyCost * count;

	public int TotalMetabolismIncrease => proto.MetabolismIncrease * count;

	public FarmFish(FarmFishInfo proto, int count = 1)
	{
		this.proto = proto;
		this.count = count;
	}

	public void Add()
	{
		count++;
	}
}
