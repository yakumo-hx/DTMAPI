using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public struct AnimalData
{
	[JsonProperty]
	[DebugInfo("成年状态标记")]
	public AnimalMatureType matureType;

	[JsonProperty]
	[DebugInfo("饱食度值", Color = "#4876bb")]
	public float energy;

	[JsonProperty]
	[DebugInfo("进食间隔计数器")]
	public int eatCounter;

	[JsonProperty]
	[DebugInfo("排泄间隔计数器")]
	public int excreteCounter;

	[JsonProperty]
	[DebugInfo("成长值", Color = "#ff4f4f")]
	public float growth;

	[JsonProperty]
	[DebugInfo]
	public int growthCounter;

	[JsonProperty]
	[DebugInfo("代谢值", Color = "#51b341")]
	public float metabolism;

	[JsonProperty]
	[DebugInfo]
	public int metabolismCounter;

	[JsonProperty]
	[DebugInfo("心情值", Color = "#df426e")]
	public int mood;

	[JsonProperty]
	[DebugInfo("今天是否进行过爱抚")]
	public bool hasFondled;

	[JsonProperty]
	[DebugInfo("繁育间隔计数器")]
	public int breedingCounter;

	public bool isAdult => matureType == AnimalMatureType.Adult;

	public bool isChild => matureType == AnimalMatureType.Child;

	[JsonConstructor]
	public AnimalData(AnimalMatureType matureType, float energy, int eatCounter, int excreteCounter, float growth, int growthCounter, float metabolism, int metabolismCounter, int mood = 70, bool hasFondled = false, int breedingCounter = 0)
	{
		this.matureType = matureType;
		this.energy = energy;
		this.eatCounter = eatCounter;
		this.excreteCounter = excreteCounter;
		this.growth = growth;
		this.growthCounter = growthCounter;
		this.metabolism = metabolism;
		this.metabolismCounter = metabolismCounter;
		this.mood = mood;
		this.hasFondled = hasFondled;
		this.breedingCounter = breedingCounter;
	}

	public bool TryCostEnergy(float value)
	{
		if (energy < value)
		{
			return false;
		}
		energy -= value;
		return true;
	}

	public void AddEnergy(float value)
	{
		energy += value;
	}
}
