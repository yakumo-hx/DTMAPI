namespace DolocTown;

public struct GeneBuffData
{
	public float GrowthMultiplier;

	public float GrowthAddition;

	public GeneBuffData(float growthMultiplier = 1f, float growthAddition = 0f)
	{
		GrowthMultiplier = growthMultiplier;
		GrowthAddition = growthAddition;
	}
}
