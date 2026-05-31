using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Plant;
using DolocTown.UI;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class PlantBasinGrass : Equipment
{
	public readonly EquipmentFuncPlantBasinGrass func;

	[JsonProperty]
	private ForageGrass[] crops;

	[JsonProperty]
	private Counter growCounter;

	private float _startPosition;

	private float _segmentSize;

	private float[] _randomOffsets;

	private readonly Counter tuCounter;

	public override bool IsValid
	{
		get
		{
			if (base.IsValid)
			{
				return func != null;
			}
			return false;
		}
	}

	public SeedInfo SeedProto => func?.SeedProto;

	[DebugInfo("牧草01")]
	public ForageGrass crop0
	{
		get
		{
			if (crops.Length == 0)
			{
				return null;
			}
			return crops[0];
		}
	}

	[DebugInfo("牧草02")]
	public ForageGrass crop1
	{
		get
		{
			if (crops.Length <= 1)
			{
				return null;
			}
			return crops[1];
		}
	}

	[DebugInfo("牧草03")]
	public ForageGrass crop2
	{
		get
		{
			if (crops.Length <= 2)
			{
				return null;
			}
			return crops[2];
		}
	}

	public IEnumerable<IFeeder> Feeders => crops;

	public PlantBasinGrass(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		func = (EquipmentFuncPlantBasinGrass)proto.Function;
		crops = new ForageGrass[func.GrassCount];
		growCounter = new Counter(func.UpdateInterval);
		tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
		ResolveCrops();
	}

	[JsonConstructor]
	public PlantBasinGrass(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, ForageGrass[] crops, Counter growCounter)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		if (proto != null)
		{
			if (!(proto.Function is EquipmentFuncPlantBasinGrass equipmentFuncPlantBasinGrass))
			{
				Debug.LogError($"PlantBasinGrass: Invalid function type {proto.Function.GetType()}");
				return;
			}
			func = equipmentFuncPlantBasinGrass;
			this.crops = crops;
			this.growCounter = growCounter ?? new Counter(equipmentFuncPlantBasinGrass.UpdateInterval);
			this.growCounter.ValidateInterval(equipmentFuncPlantBasinGrass.UpdateInterval);
			tuCounter = DolocAPI.GlobalParameter.NewTuCounter;
			ResolveCrops();
		}
	}

	public override void AfterLoadEquipment()
	{
		base.AfterLoadEquipment();
		ForageGrass[] array = crops;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AfterLoadData(this);
		}
	}

	protected override void CalcSizeInfo()
	{
		base.CalcSizeInfo();
		if (proto.Function is EquipmentFuncPlantBasinGrass equipmentFuncPlantBasinGrass)
		{
			_startPosition = base.WidthLevel.x;
			float num = base.WidthLevel.z - base.WidthLevel.x;
			_segmentSize = num / (float)equipmentFuncPlantBasinGrass.GrassCount;
			_randomOffsets = new float[equipmentFuncPlantBasinGrass.GrassCount];
			for (int i = 0; i < _randomOffsets.Length; i++)
			{
				_randomOffsets[i] = UnityEngine.Random.Range(-0.2f, 0.2f);
			}
		}
	}

	private void ResolveCrops()
	{
		if (crops.Length != func.GrassCount)
		{
			ForageGrass[] destinationArray = new ForageGrass[func.GrassCount];
			Array.Copy(crops, destinationArray, Mathf.Min(func.GrassCount, crops.Length));
			crops = destinationArray;
		}
		ForageGrass[] array;
		for (int i = 0; i < crops.Length; i++)
		{
			array = crops;
			int num = i;
			if (array[num] == null)
			{
				array[num] = new ForageGrass(func.SeedProto, this);
			}
		}
		array = crops;
		for (int num = 0; num < array.Length; num++)
		{
			array[num].ResetPosition();
		}
	}

	public bool TryIndex(ForageGrass data, out int index)
	{
		index = -1;
		if (data == null)
		{
			return false;
		}
		index = Array.IndexOf(crops, data);
		return index >= 0;
	}

	public Vector3 GetCropPosition(int index)
	{
		return new Vector3(_startPosition + ((float)index + 0.5f) * _segmentSize + _randomOffsets[index], base.HeightLevel.z, -0.8f);
	}

	public void DEBUG_SetLevel(int lv)
	{
		if (!crops.IsNullOrEmpty())
		{
			ForageGrass[] array = crops;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].DEBUG_SetLevel(lv);
			}
		}
	}

	protected override void Update()
	{
		if (tuCounter.Tick() && growCounter.Tick())
		{
			ForageGrass[] array = crops;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Grow();
			}
		}
	}

	protected override void UpdateNoRender()
	{
		if (tuCounter.Tick() && growCounter.Tick())
		{
			ForageGrass[] array = crops;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].GrowNoRender();
			}
		}
	}

	protected override void OnRender()
	{
		base.OnRender();
		ForageGrass[] array = crops;
		foreach (ForageGrass forageGrass in array)
		{
			if (forageGrass != null)
			{
				forageGrass.ResetPosition();
				forageGrass.RenderCrop();
			}
		}
	}

	protected override void OnUnRender()
	{
		ForageGrass[] array = crops;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ClearRender();
		}
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		base.RetrieveItemOnRemoval(putInBackpack);
		ForageGrass[] array = crops;
		foreach (ForageGrass forageGrass in array)
		{
			if (forageGrass.currentLevel != 0)
			{
				if (forageGrass.IsMature)
				{
					forageGrass.Harvest(shouldRender: false, putInBackpack);
				}
				else
				{
					this.PlaceItemInBagOrCreateDropItem(DolocAPI.GlobalParameter.ItemRefWeeds, putInBackpack, sendMessage: true);
				}
			}
		}
	}

	protected override void OnTouch()
	{
		if (crops.Any((ForageGrass x) => x.IsMature))
		{
			this.ShowSceneOperationTip(PositionTip, DolocConfig.StaticTexts.UiOperationHarvest);
		}
	}

	protected override void OnDisTouch()
	{
		this.HideSceneOperationTip();
	}

	protected override void OnInteract()
	{
		base.OnInteract();
		if (!crops.Any((ForageGrass x) => x.IsMature))
		{
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			ForageGrass[] array = crops;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Harvest(shouldRender: true);
			}
			this.HideSceneOperationTip();
		});
	}
}
