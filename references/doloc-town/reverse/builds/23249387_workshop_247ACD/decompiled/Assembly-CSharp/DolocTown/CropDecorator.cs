using System.Linq;
using UnityEngine;

namespace DolocTown;

public class CropDecorator : ICropFunction
{
	private readonly Crop crop;

	public readonly CropGeneFunction[] functions;

	private readonly ICropFunction composedFunction;

	public bool IsNaturalMoist => functions.Any((CropGeneFunction x) => x.IsNaturalMoist);

	public int DctMoodContribution => composedFunction.MoodContribution;

	public int DctMaxLifespan => composedFunction.MaxLifespan;

	public int MoodContribution => crop.OriginMoodContribution;

	public int MaxLifespan => crop.OriginMaxLifespan;

	public CropDecorator(Crop crop, CropGeneFunction[] functions)
	{
		this.crop = crop;
		this.functions = functions;
		composedFunction = ComposeFunction(functions);
	}

	private ICropFunction ComposeFunction(CropGeneFunction[] functions)
	{
		if (functions.IsNullOrEmpty())
		{
			return this;
		}
		ICropFunction cropFunction = this;
		foreach (CropGeneFunction obj in functions)
		{
			obj.ComposeFunction(cropFunction);
			cropFunction = obj;
		}
		return cropFunction;
	}

	public bool CheckOxygen(Vector2Int agentPositionCell, out int value)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].CheckOxygen(agentPositionCell, out value))
			{
				return true;
			}
		}
		value = 0;
		return false;
	}

	public void OnCropDead(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnCropDead(shouldRender);
		}
	}

	public void OnCropMature(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnCropMature(shouldRender);
		}
	}

	public void OnCropLevelUp(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnCropLevelUp(shouldRender);
		}
	}

	public void OnCropLevelDown(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnCropLevelDown(shouldRender);
		}
	}

	public void OnWater(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnWater(shouldRender);
		}
	}

	public void OnProtected(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnProtected(shouldRender);
		}
	}

	public void UpdateEx(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateEx(shouldRender);
		}
	}

	public void OnRender()
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnRender();
		}
	}

	public void OnUnRender()
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnUnRender();
		}
	}

	public void AfterHarvest(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AfterHarvest(shouldRender);
		}
	}

	public void AfterClearWither(bool shouldRender)
	{
		CropGeneFunction[] array = functions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AfterClearWither(shouldRender);
		}
	}

	public bool DctRegrow(bool shouldRender)
	{
		if (!composedFunction.Regrow(shouldRender))
		{
			return false;
		}
		OnCropLevelDown(shouldRender);
		return true;
	}

	public bool DctGrowBack(bool shouldRender, bool shouldClearGrowth)
	{
		if (!composedFunction.GrowBack(shouldRender, shouldClearGrowth))
		{
			return false;
		}
		OnCropLevelDown(shouldRender);
		return true;
	}

	public bool DctGrowForward(bool shouldRender, bool shouldClearGrowth)
	{
		if (!composedFunction.GrowForward(shouldRender, shouldClearGrowth))
		{
			return false;
		}
		OnCropLevelUp(shouldRender);
		return true;
	}

	public void DctWater(bool shouldRender, bool invokeCallback)
	{
		composedFunction.Water(shouldRender, invokeCallback);
		if (invokeCallback)
		{
			OnWater(shouldRender);
		}
	}

	public void DctProtected(bool shouldRender)
	{
		composedFunction.Protected(shouldRender);
		OnProtected(shouldRender);
	}

	public void DctUpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		crop.lastAddition = addition;
		crop.lastFertilizerAddition = fertilizerAddition;
		crop.hasGrowed = false;
		composedFunction.UpdateNormal(shouldRender, isMoist, addition, fertilizerAddition);
		UpdateEx(shouldRender);
	}

	public void DctUpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		crop.lastAddition = addition;
		crop.lastFertilizerAddition = fertilizerAddition;
		crop.hasGrowed = false;
		composedFunction.UpdateAcidRain(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
		UpdateEx(shouldRender);
	}

	public void DctUpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		crop.lastAddition = addition;
		crop.lastFertilizerAddition = fertilizerAddition;
		crop.hasGrowed = false;
		composedFunction.UpdateScorchSun(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
		UpdateEx(shouldRender);
	}

	public void DctThunder(bool shouldRender)
	{
		composedFunction.Thunder(shouldRender);
	}

	public bool DctCheckGrowthMonth(bool shouldRender)
	{
		return composedFunction.CheckGrowthMonth(shouldRender);
	}

	public void DctGenCropOutput(bool putInBackpack)
	{
		composedFunction.GenCropOutput(putInBackpack);
	}

	public bool DctTryGetNeedWaterOrClear(out bool value)
	{
		return composedFunction.TryGetNeedWaterOrClear(out value);
	}

	public bool Regrow(bool shouldRender)
	{
		return crop.OriginRegrow(shouldRender);
	}

	public bool GrowBack(bool shouldRender, bool shouldClearGrowth)
	{
		return crop.OriginGrowBack(shouldRender, shouldClearGrowth);
	}

	public bool GrowForward(bool shouldRender, bool shouldClearGrowth)
	{
		return crop.OriginGrowForward(shouldRender, shouldClearGrowth);
	}

	public void Water(bool shouldRender, bool invokeCallback)
	{
		crop.OriginWater(shouldRender);
	}

	public void Protected(bool shouldRender)
	{
		crop.OriginProtected(shouldRender);
	}

	public void UpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition)
	{
		crop.OriginUpdateNormal(shouldRender, isMoist, addition, fertilizerAddition);
	}

	public void UpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		crop.OriginUpdateAcidRain(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
	}

	public void UpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage)
	{
		crop.OriginUpdateScorchSun(shouldRender, isMoist, addition, fertilizerAddition, isProtected, damage);
	}

	public void Thunder(bool shouldRender)
	{
		crop.OriginThunder(shouldRender);
	}

	public bool CheckGrowthMonth(bool shouldRender)
	{
		return crop.OriginCheckGrowthMonth(shouldRender);
	}

	public void GenCropOutput(bool putInBackpack)
	{
		crop.OriginGenCropOutput(putInBackpack);
	}

	public bool TryGetNeedWaterOrClear(out bool value)
	{
		return crop.OriginTryGetNeedWaterOrClear(out value);
	}
}
