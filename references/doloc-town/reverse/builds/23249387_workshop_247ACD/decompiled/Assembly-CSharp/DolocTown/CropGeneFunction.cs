using System;
using DolocTown.Config;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public abstract class CropGeneFunction : ICropFunction
{
	public readonly CropGeneInfo geneProto;

	protected ICropFunction function { get; private set; }

	protected Crop crop { get; private set; }

	protected CropRenderer Renderer => crop.Renderer;

	protected SeedInfo SeedProto => crop.seedProto;

	protected Room CurrentRoom => crop.plantBasin.CurrentRoom;

	protected CropData Data => crop.data;

	public virtual bool IsValid => geneProto != null;

	[JsonProperty]
	public string geneId => geneProto.Id;

	public virtual int MoodContribution => 0;

	public virtual int MaxLifespan => SeedProto.Lifespan;

	public virtual bool IsNaturalMoist => false;

	protected CropGeneFunction(CropGeneInfo geneProto)
	{
		this.geneProto = geneProto;
	}

	[JsonConstructor]
	protected CropGeneFunction(string geneId)
	{
		if (DolocConfig.Tables.TbCropGene.DataMap.TryGetValue(geneId, out var value))
		{
			geneProto = value;
		}
	}

	public void ComposeFunction(ICropFunction function)
	{
		this.function = function;
	}

	public void BindCrop(Crop crop)
	{
		if (crop != null && this.crop == null)
		{
			this.crop = crop;
		}
	}

	public virtual void AfterLoadData()
	{
	}

	public virtual void OnCreate()
	{
	}

	public void OverrideCropData(Func<CropData, CropData> func)
	{
		crop.SetCropData(func(crop.data));
	}

	public virtual CropOutputData HandleOutputData(CropOutputData outputData, RangedItem item)
	{
		return outputData;
	}

	public virtual float HandleBuffData(float originValue)
	{
		return originValue;
	}

	public virtual bool Regrow(bool shouldRender)
	{
		return function.Regrow(shouldRender);
	}

	public virtual bool GrowBack(bool shouldRender, bool shouldClearGrowth)
	{
		return function.GrowBack(shouldRender, shouldClearGrowth);
	}

	public virtual bool GrowForward(bool shouldRender, bool shouldClearGrowth)
	{
		return function.GrowForward(shouldRender, shouldClearGrowth);
	}

	public virtual void Water(bool shouldRender, bool invokeCallback)
	{
		function.Water(shouldRender, invokeCallback);
	}

	public virtual void Protected(bool shouldRender)
	{
		function.Protected(shouldRender);
	}

	public virtual void UpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		function.UpdateNormal(shouldRender, isMoist, addition, fertilizerAddition);
	}

	public virtual void UpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		function.UpdateAcidRain(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
	}

	public virtual void UpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		function.UpdateScorchSun(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
	}

	public virtual void Thunder(bool shouldRender)
	{
		function.Thunder(shouldRender);
	}

	public virtual bool CheckGrowthMonth(bool shouldRender)
	{
		return function.CheckGrowthMonth(shouldRender);
	}

	public virtual void GenCropOutput(bool putInBackpack)
	{
		function.GenCropOutput(putInBackpack);
	}

	public virtual bool TryGetNeedWaterOrClear(out bool value)
	{
		return function.TryGetNeedWaterOrClear(out value);
	}

	public virtual void OnWater(bool shouldRender)
	{
	}

	public virtual void OnProtected(bool shouldRender)
	{
	}

	public virtual void OnRender()
	{
	}

	public virtual void OnUnRender()
	{
	}

	public virtual void UpdateEx(bool shouldRender)
	{
	}

	public virtual bool CheckOxygen(Vector2Int agentPositionCell, out int value)
	{
		value = 0;
		return false;
	}

	public virtual void AfterHarvest(bool shouldRender)
	{
	}

	public virtual void AfterClearWither(bool shouldRender)
	{
	}

	public virtual void OnCropMature(bool shouldRender)
	{
	}

	public virtual void OnCropDead(bool shouldRender)
	{
	}

	public virtual void OnCropLevelUp(bool shouldRender)
	{
	}

	public virtual void OnCropLevelDown(bool shouldRender)
	{
	}
}
