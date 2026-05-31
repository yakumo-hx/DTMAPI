using DolocTown.Config.Plant;
using Newtonsoft.Json;

namespace DolocTown;

public class CropGeneFunctionConiferLeaf : CropGeneFunction
{
	private readonly CropGeneFuncProtoConiferLeaf _func;

	public CropGeneFunctionConiferLeaf(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoConiferLeaf)geneProto.Function;
	}

	[JsonConstructor]
	protected CropGeneFunctionConiferLeaf(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoConiferLeaf)geneProto.Function;
		}
	}

	public override void UpdateEx(bool shouldRender)
	{
		if (!base.crop.plantBasin.Supply.IsMoist)
		{
			base.crop.plantBasin.Water(shouldRender: false, sendMessage: false, invokeGeneCallback: false);
		}
	}

	public override void UpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		if (base.Data.isDead)
		{
			if (shouldRender)
			{
				base.Renderer.SetMoist(isMoist: false);
			}
			return;
		}
		if (shouldRender)
		{
			base.crop.Renderer.SetMoist(isMoist, base.Data.isPolluted);
		}
		base.crop.Grow(shouldRender, isMoist, addition, fertilizerAddition);
	}

	public override void UpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		if (base.Data.isDead)
		{
			if (shouldRender)
			{
				base.Renderer.SetMoist(isMoist: false);
			}
		}
		else if (isProtected)
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
		}
		else
		{
			base.crop.RefreshMoistStatus(isMoist: true, shouldRender);
			base.crop.Grow(shouldRender, isMoist: true, addition, fertilizerAddition);
		}
	}

	public override float HandleBuffData(float originValue)
	{
		return originValue - _func.GrowthDecrease;
	}

	public override bool TryGetNeedWaterOrClear(out bool value)
	{
		value = base.Data.isDead;
		return true;
	}
}
