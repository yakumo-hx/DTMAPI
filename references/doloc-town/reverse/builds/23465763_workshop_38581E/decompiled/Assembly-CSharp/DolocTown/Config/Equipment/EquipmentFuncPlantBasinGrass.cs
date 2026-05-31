using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Plant;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPlantBasinGrass : EquipmentFuncEquipment
{
	public const int __ID__ = 1276740246;

	private SeedInfo _proto;

	public int GrassCount { get; private set; }

	public string CropName { get; private set; }

	public int CropSkinIndex { get; private set; }

	public int UpdateInterval { get; private set; }

	public SeedInfo SeedProto
	{
		get
		{
			if (_proto != null)
			{
				return _proto;
			}
			if (CropName.IsNullOrEmpty() || !DolocConfig.Tables.TbSeed.DataMap.TryGetValue(CropName, out var value))
			{
				_proto = DolocConfig.Tables.TbSeed.DataList[0];
			}
			else
			{
				_proto = value;
			}
			return _proto;
		}
	}

	public EquipmentFuncPlantBasinGrass(JSONNode _json)
		: base(_json)
	{
		if (!_json["grass_count"].IsNumber)
		{
			throw new SerializationException();
		}
		GrassCount = _json["grass_count"];
		if (!_json["crop_name"].IsString)
		{
			throw new SerializationException();
		}
		CropName = _json["crop_name"];
		if (!_json["crop_skin_index"].IsNumber)
		{
			throw new SerializationException();
		}
		CropSkinIndex = _json["crop_skin_index"];
		if (!_json["update_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		UpdateInterval = _json["update_interval"];
	}

	public EquipmentFuncPlantBasinGrass(int grass_count, string crop_name, int crop_skin_index, int update_interval)
	{
		GrassCount = grass_count;
		CropName = crop_name;
		CropSkinIndex = crop_skin_index;
		UpdateInterval = update_interval;
	}

	public static EquipmentFuncPlantBasinGrass DeserializeEquipmentFuncPlantBasinGrass(JSONNode _json)
	{
		return new EquipmentFuncPlantBasinGrass(_json);
	}

	public override int GetTypeId()
	{
		return 1276740246;
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
		return "{ GrassCount:" + GrassCount + ",CropName:" + CropName + ",CropSkinIndex:" + CropSkinIndex + ",UpdateInterval:" + UpdateInterval + ",}";
	}
}
