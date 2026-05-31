using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Fishing;
using RedSaw;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[DebugObject]
public class FarmFishShoal
{
	private readonly Dictionary<string, FarmFish> fishes = new Dictionary<string, FarmFish>();

	[DebugInfo("鱼的总数")]
	public int TotalFishCount => fishes.Sum((KeyValuePair<string, FarmFish> x) => x.Value.count);

	[DebugInfo("鱼的种类数")]
	public int TotalFishClassCount => fishes.Values.Count;

	[DebugInfo("总食量消耗")]
	public int TotalEnergyCost { get; private set; }

	[DebugInfo("总代谢速度")]
	public int TotalMetabolismIncrease { get; private set; }

	public int GetMetabolism(int energy)
	{
		if (energy <= 0)
		{
			return 0;
		}
		if (energy >= TotalEnergyCost)
		{
			return TotalMetabolismIncrease;
		}
		return (int)((float)energy / (float)TotalEnergyCost * (float)TotalMetabolismIncrease);
	}

	public void Clear()
	{
		fishes.Clear();
		TotalEnergyCost = 0;
		TotalMetabolismIncrease = 0;
	}

	public void UpdateStatus()
	{
		TotalEnergyCost = 0;
		TotalMetabolismIncrease = 0;
		if (fishes.Count == 0)
		{
			return;
		}
		foreach (FarmFish value in fishes.Values)
		{
			TotalEnergyCost += value.TotalEnergyCost;
			TotalMetabolismIncrease += value.TotalMetabolismIncrease;
		}
	}

	public FarmFishInfo RollFish()
	{
		if (fishes.Count == 0)
		{
			return null;
		}
		FarmFish[] array = fishes.Values.ToArray();
		int value;
		int num = RandomUtils.RussianRoulette(array.Select((FarmFish x) => x.count).ToArray(), out value);
		return array[num].proto;
	}

	public void AddFish(string fishName)
	{
		FarmFishInfo value2;
		if (fishes.TryGetValue(fishName, out var value))
		{
			value.Add();
		}
		else if (DolocConfig.Tables.TbFarmFish.DataMap.TryGetValue(fishName, out value2))
		{
			value = new FarmFish(value2);
			fishes.Add(fishName, value);
		}
	}
}
