using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionAntiAcidRain : CropGeneFunction
{
	public CropGeneFunctionAntiAcidRain(CropGeneInfo geneProto)
		: base(geneProto)
	{
	}

	[JsonConstructor]
	protected CropGeneFunctionAntiAcidRain(string geneId)
		: base(geneId)
	{
	}

	public override void UpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		if (base.crop.isDead)
		{
			if (shouldRender)
			{
				base.Renderer.SetMoist(isMoist: false);
			}
			return;
		}
		if (isProtected)
		{
			if (base.Data.isPolluted)
			{
				OverrideCropData(delegate(CropData data)
				{
					data.isPolluted = false;
					return data;
				});
				if (shouldRender)
				{
					base.Renderer.SetMoist(isMoist);
				}
			}
			base.crop.Grow(shouldRender, isMoist, addition, fertilizerAddition);
			return;
		}
		if (base.Data.isPolluted)
		{
			OverrideCropData(delegate(CropData data)
			{
				data.isMoist = true;
				data.isPolluted = false;
				return data;
			});
			if (shouldRender)
			{
				base.Renderer.SetMoist(isMoist: true);
			}
		}
		base.crop.Grow(shouldRender, isMoist: true, addition, fertilizerAddition);
		base.crop.plantBasin.Water(shouldRender: false, sendMessage: false, invokeGeneCallback: false);
	}

	public override CropOutputData HandleOutputData(CropOutputData outputData, RangedItem item)
	{
		outputData.originCount = item.minCount;
		return outputData;
	}
}
