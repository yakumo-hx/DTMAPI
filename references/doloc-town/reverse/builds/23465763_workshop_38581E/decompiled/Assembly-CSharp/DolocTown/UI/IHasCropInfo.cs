namespace DolocTown.UI;

public interface IHasCropInfo
{
	bool HasCrop { get; }

	string CropTitle { get; }

	string GrowthLevelInfo { get; }

	string HarvestCountInfo { get; }

	string GeneInfo { get; }

	float GrowthProgress { get; }

	float HealthProgress { get; }

	float WaterProgress { get; }

	float FilmProgress { get; }

	float FertilizerProgress { get; }

	bool IsLighting { get; }
}
