using DolocTown.Config;
using DolocTown.Config.Plant;

namespace DolocTown;

public interface IHasGeneGroup
{
	bool HasGene { get; }

	bool HasUnnaturalGenes { get; }

	bool IsCloned { get; }

	GeneGroup GeneGroup { get; }

	int GeneCount => GeneGroup.GeneCount;

	void SetGeneGroup(GeneGroup group)
	{
		GeneGroup?.SetGenes(group?.Genes);
	}

	void AddGene(string id)
	{
		CropGeneInfo orDefault = DolocConfig.Tables.TbCropGene.GetOrDefault(id ?? "");
		if (orDefault != null)
		{
			GeneGroup?.AddGene(orDefault);
		}
	}

	GeneGroup GetDefaultGeneGroup();
}
