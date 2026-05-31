using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncShowerRoom : EquipmentFuncEquipment
{
	public const int __ID__ = -1024350505;

	public int WaterCost { get; private set; }

	public string BuffId { get; private set; }

	public float HealthRecv { get; private set; }

	public float EnergyRecv { get; private set; }

	public SpriteAsset Mask { get; private set; }

	public EquipmentFuncShowerRoom(JSONNode _json)
		: base(_json)
	{
		if (!_json["water_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		WaterCost = _json["water_cost"];
		if (!_json["buff_id"].IsString)
		{
			throw new SerializationException();
		}
		BuffId = _json["buff_id"];
		if (!_json["health_recv"].IsNumber)
		{
			throw new SerializationException();
		}
		HealthRecv = _json["health_recv"];
		if (!_json["energy_recv"].IsNumber)
		{
			throw new SerializationException();
		}
		EnergyRecv = _json["energy_recv"];
		if (!_json["mask"].IsObject)
		{
			throw new SerializationException();
		}
		Mask = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["mask"]));
	}

	public EquipmentFuncShowerRoom(int water_cost, string buff_id, float health_recv, float energy_recv, SpriteAsset mask)
	{
		WaterCost = water_cost;
		BuffId = buff_id;
		HealthRecv = health_recv;
		EnergyRecv = energy_recv;
		Mask = mask;
	}

	public static EquipmentFuncShowerRoom DeserializeEquipmentFuncShowerRoom(JSONNode _json)
	{
		return new EquipmentFuncShowerRoom(_json);
	}

	public override int GetTypeId()
	{
		return -1024350505;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ WaterCost:" + WaterCost + ",BuffId:" + BuffId + ",HealthRecv:" + HealthRecv + ",EnergyRecv:" + EnergyRecv + ",Mask:" + Mask?.ToString() + ",}";
	}
}
