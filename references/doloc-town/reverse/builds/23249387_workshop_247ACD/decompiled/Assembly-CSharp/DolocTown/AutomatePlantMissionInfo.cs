namespace DolocTown;

public readonly struct AutomatePlantMissionInfo
{
	public readonly PlantCondition condition;

	public readonly PlantBasin plantBasin;

	public readonly Case seedContainer;

	public AutomatePlantMissionInfo(PlantCondition condition, PlantBasin plantBasin, Case seedContainer)
	{
		this.plantBasin = plantBasin;
		this.seedContainer = seedContainer;
		this.condition = condition;
	}
}
