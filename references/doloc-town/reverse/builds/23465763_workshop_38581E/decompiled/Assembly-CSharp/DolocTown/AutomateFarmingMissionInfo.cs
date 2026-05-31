namespace DolocTown;

public readonly struct AutomateFarmingMissionInfo
{
	public readonly PlantBasin plantBasin;

	public readonly Case container;

	public AutomateFarmingMissionInfo(PlantBasin plantBasin, Case container)
	{
		this.plantBasin = plantBasin;
		this.container = container;
	}
}
