namespace DolocTown.UI;

public struct CropInfoData : IUIData
{
	public string title;

	public string growthLevelInfo;

	public string harvestInfo;

	public string geneInfo;

	public float growthProgress;

	public float healthProgress;

	public float waterProgress;

	public float filmProgress;

	public float fertilizerProgress;

	public bool isLighting;

	public bool notEmpty { get; }

	public CropInfoData(IHasCropInfo plantBasin)
	{
		this = default(CropInfoData);
		if (plantBasin != null)
		{
			notEmpty = true;
			title = plantBasin.CropTitle;
			growthLevelInfo = plantBasin.GrowthLevelInfo;
			harvestInfo = plantBasin.HarvestCountInfo;
			geneInfo = plantBasin.GeneInfo;
			growthProgress = plantBasin.GrowthProgress;
			healthProgress = plantBasin.HealthProgress;
			waterProgress = plantBasin.WaterProgress;
			filmProgress = plantBasin.FilmProgress;
			fertilizerProgress = plantBasin.FertilizerProgress;
			isLighting = plantBasin.IsLighting;
		}
	}
}
