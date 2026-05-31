using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class PlantBasinSimple : PlantBasin
{
	private new readonly EquipmentFuncPlantBasinSimple func;

	[JsonProperty]
	[DebugInfo("是否有作物达到过收获阶段")]
	private bool hasCropReachedHarvestStage;

	[DebugInfo("塑料薄膜护盾值", AllowEdit = true)]
	protected int ProtectorValue
	{
		get
		{
			return supply.ProtectedCounter;
		}
		set
		{
			supply.ProtectedCounter = value;
		}
	}

	public PlantBasinSimple(IEquipmentHost room, int id, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(room, id, proto, wp, anchor, turn)
	{
		func = proto.Function as EquipmentFuncPlantBasinSimple;
		supply = new PlantBasinSupply(func.SupplyCapacity);
		hasCropReachedHarvestStage = false;
	}

	[JsonConstructor]
	protected PlantBasinSimple(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn, Crop crop, PlantBasinSupply supply, Counter updateCounter = null, bool hasCropReachedHarvestStage = false)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn, crop, supply, updateCounter)
	{
		base.crop = crop;
		base.supply = supply;
		this.hasCropReachedHarvestStage = hasCropReachedHarvestStage;
		func = proto.Function as EquipmentFuncPlantBasinSimple;
	}

	public void MarkAsReeachMatureStage()
	{
		hasCropReachedHarvestStage = true;
	}

	protected override void OnRemove()
	{
		if (!base.IsRender)
		{
			return;
		}
		if (crop == null)
		{
			if (hasCropReachedHarvestStage)
			{
				DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.PLANTBASIN_CRACK);
			}
			return;
		}
		if (crop.hasHarvested || crop.isMature)
		{
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.PLANTBASIN_CRACK);
		}
		ClearCrop();
		UpdateCropShinyRenderer();
	}

	protected override void AfterHarvest(bool isDead = false)
	{
		if (isDead)
		{
			ClearCrop();
		}
		else if (!crop.Regrow(base.IsRender))
		{
			hasCropReachedHarvestStage = true;
			ClearCrop();
			base.Host.RemoveEquipment(this);
		}
	}

	public void DestroyImmediately()
	{
		if (base.IsRender)
		{
			DolocAPI.RaiseInstantPSEffects(base.PositionCenter, InstantParticleEffectsType.PLANTBASIN_CRACK);
		}
		DolocAPI.DelayFrame(delegate
		{
			ClearCrop();
			MarkAsReeachMatureStage();
			base.Host.RemoveEquipment(this);
		});
	}

	public override void RetrieveItemOnRemoval(bool putInBackpack)
	{
		if (crop == null)
		{
			if (!hasCropReachedHarvestStage)
			{
				this.PlaceItemInBagOrCreateDropItem(proto.Id, putInBackpack, sendMessage: false);
			}
			return;
		}
		GenerateCropOutputOnRemoved(putInBackpack);
		if (!crop.hasHarvested && !crop.isMature)
		{
			this.PlaceItemInBagOrCreateDropItem(proto.Id, putInBackpack, sendMessage: false);
		}
	}
}
