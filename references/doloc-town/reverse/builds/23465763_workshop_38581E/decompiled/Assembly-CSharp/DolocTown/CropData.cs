using Newtonsoft.Json;

namespace DolocTown;

public struct CropData
{
	public bool isMature;

	public bool isDead;

	public bool isMoist;

	public int currentLevel;

	public int lifespan;

	public float currentGrowthValue;

	public float currentHealthValue;

	public bool isPolluted;

	public int harvestTimes;

	public CropData(int lifespan, float healthValue)
	{
		this.lifespan = lifespan;
		isMature = false;
		isDead = false;
		isMoist = false;
		currentLevel = 0;
		currentGrowthValue = 0f;
		currentHealthValue = healthValue;
		isPolluted = false;
		harvestTimes = 0;
	}

	[JsonConstructor]
	public CropData(bool isMature, bool isDead, int currentLevel, int lifespan, float currentGrowthValue, float currentHealthValue, bool isMoist, bool isPolluted, int harvestTimes = 0)
	{
		this.isMature = isMature;
		this.isDead = isDead;
		this.currentLevel = currentLevel;
		this.lifespan = lifespan;
		this.currentGrowthValue = currentGrowthValue;
		this.currentHealthValue = currentHealthValue;
		this.isMoist = isMoist;
		this.isPolluted = isPolluted;
		this.harvestTimes = harvestTimes;
	}
}
