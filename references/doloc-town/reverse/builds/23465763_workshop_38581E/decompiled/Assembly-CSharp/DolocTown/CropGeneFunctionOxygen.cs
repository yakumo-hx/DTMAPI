using System.Collections.Generic;
using DolocTown.Config.Plant;
using Newtonsoft.Json;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class CropGeneFunctionOxygen : CropGeneFunction
{
	private CropGeneFuncProtoOxygen _func;

	private HashSet<Vector2Int> aroundPositions;

	private GameEntitySlot<EquipmentParticleSystemRenderer> psSlot = new GameEntitySlot<EquipmentParticleSystemRenderer>("oxygen");

	private Counter counter;

	public CropGeneFunctionOxygen(CropGeneInfo geneProto)
		: base(geneProto)
	{
		_func = (CropGeneFuncProtoOxygen)geneProto.Function;
		counter = new Counter(_func.Interval);
	}

	[JsonConstructor]
	public CropGeneFunctionOxygen(string geneId)
		: base(geneId)
	{
		if (geneProto != null)
		{
			_func = (CropGeneFuncProtoOxygen)geneProto.Function;
			counter = new Counter(_func.Interval);
		}
	}

	public override void AfterLoadData()
	{
		base.AfterLoadData();
		aroundPositions = new HashSet<Vector2Int>(base.crop.plantBasin.GetAffectedPositions(_func.HorizontalRange, _func.VerticalRangeTop, _func.VerticalRangeBottom));
	}

	public override void OnCreate()
	{
		aroundPositions = new HashSet<Vector2Int>(base.crop.plantBasin.GetAffectedPositions(_func.HorizontalRange, _func.VerticalRangeTop, _func.VerticalRangeBottom));
	}

	public override void OnRender()
	{
		base.OnRender();
		if (base.crop.CurrentLevel >= _func.LevelThreshold)
		{
			psSlot.Do(delegate(EquipmentParticleSystemRenderer ps)
			{
				ps.position2d = base.crop.plantBasin.PositionTop;
				ps.Play();
			});
		}
	}

	public override void OnCropLevelUp(bool shouldRender)
	{
		base.OnCropLevelUp(shouldRender);
		if (shouldRender && base.crop.CurrentLevel >= _func.LevelThreshold)
		{
			psSlot.Do(delegate(EquipmentParticleSystemRenderer ps)
			{
				ps.position2d = base.crop.plantBasin.PositionTop;
				ps.Play();
			});
		}
	}

	public override void OnCropLevelDown(bool shouldRender)
	{
		base.OnCropLevelDown(shouldRender);
		if (shouldRender && base.crop.CurrentLevel < _func.LevelThreshold)
		{
			psSlot.Release();
		}
	}

	public override void OnUnRender()
	{
		base.OnUnRender();
		psSlot.Release();
	}

	public override bool CheckOxygen(Vector2Int agentPositionCell, out int value)
	{
		value = 0;
		if (base.crop.CurrentLevel < _func.LevelThreshold)
		{
			return false;
		}
		if (!aroundPositions.Contains(agentPositionCell))
		{
			return false;
		}
		if (!counter.Tick())
		{
			return false;
		}
		value = 1;
		return true;
	}
}
