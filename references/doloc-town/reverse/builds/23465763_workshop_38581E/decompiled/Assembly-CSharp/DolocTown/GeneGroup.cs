using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class GeneGroup
{
	private readonly HashSet<CropGeneInfo> geneProtos = new HashSet<CropGeneInfo>();

	[JsonProperty]
	public string[] geneIds => geneProtos.Select((CropGeneInfo x) => x.Id).ToArray();

	public CropGeneInfo[] Genes => geneProtos.ToArray();

	public int GeneCount => geneProtos.Count;

	public bool IsEmpty => geneProtos.Count == 0;

	public GeneGroup(CropGeneInfo[] genes)
	{
		SetGenes(genes);
	}

	[JsonConstructor]
	public GeneGroup(string[] geneIds = null)
	{
		if (geneIds == null)
		{
			return;
		}
		foreach (string key in geneIds)
		{
			if (DolocConfig.Tables.TbCropGene.DataMap.TryGetValue(key, out var value))
			{
				AddGene(value);
			}
		}
	}

	public GeneGroup Clone()
	{
		return new GeneGroup(geneProtos.ToArray());
	}

	public bool ContainsGene(string geneId)
	{
		return geneProtos.Any((CropGeneInfo g) => g.Id == geneId);
	}

	public bool ContainsGenes(CropGeneInfo[] genes)
	{
		if (!genes.IsNullOrEmpty())
		{
			return genes.All((CropGeneInfo gene) => geneProtos.Contains(gene));
		}
		return false;
	}

	public void RollGene(SeedInfo proto, bool clearExisting)
	{
		if (clearExisting)
		{
			geneProtos.Clear();
		}
		if (proto.TryRollGene(out var geneProto))
		{
			AddGene(geneProto);
		}
	}

	public GeneGroup Compress(GeneGroup naturalGeneGroup)
	{
		return new GeneGroup(geneProtos.ToArray().GetInheritedGenes(naturalGeneGroup));
	}

	public GeneGroup Synthesize(GeneGroup geneGroup)
	{
		return new GeneGroup(geneProtos.ToArray().SynthesizeGenes(geneGroup.geneProtos.ToArray()));
	}

	private void ResortGenes()
	{
	}

	public bool AddGene(CropGeneInfo geneProto)
	{
		if (geneProto == null)
		{
			return false;
		}
		if (geneProtos.Count < DolocAPI.GlobalParameter.MaxGeneCount)
		{
			return geneProtos.Add(geneProto);
		}
		return false;
	}

	public void RemoveGene(CropGeneInfo gene)
	{
		geneProtos.Remove(gene);
	}

	public void Clear()
	{
		geneProtos.Clear();
	}

	public void SetGenes(CropGeneInfo[] genes)
	{
		geneProtos.Clear();
		if (!genes.IsNullOrEmpty())
		{
			foreach (CropGeneInfo geneProto in genes)
			{
				AddGene(geneProto);
			}
		}
	}

	public bool IsSame(GeneGroup other)
	{
		if (other?.geneProtos != null)
		{
			return geneProtos.SetEquals(other.geneProtos);
		}
		return false;
	}

	public bool IsSame(CropGeneInfo[] genes)
	{
		if (genes != null)
		{
			return geneProtos.SetEquals(genes);
		}
		return false;
	}

	public override string ToString()
	{
		if (IsEmpty)
		{
			return "无基因";
		}
		return string.Join(", ", geneProtos.Select((CropGeneInfo g) => g.Title));
	}
}
