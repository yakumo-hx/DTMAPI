using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public abstract class CropGeneFuncProto : BeanBase
{
	public CropGeneFuncProto(JSONNode _json)
	{
	}

	public CropGeneFuncProto()
	{
	}

	public static CropGeneFuncProto DeserializeCropGeneFuncProto(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"CropGeneFuncProtoWindSow" => new CropGeneFuncProtoWindSow(_json), 
			"CropGeneFuncProtoSymbioticSupply" => new CropGeneFuncProtoSymbioticSupply(_json), 
			"CropGeneFuncProtoParasite" => new CropGeneFuncProtoParasite(_json), 
			"CropGeneFuncProtoSprinkler" => new CropGeneFuncProtoSprinkler(_json), 
			"CropGeneFuncProtoOxygen" => new CropGeneFuncProtoOxygen(_json), 
			"CropGeneFuncProtoFractalCrop" => new CropGeneFuncProtoFractalCrop(_json), 
			"CropGeneFuncProtoImmortalJellyfish" => new CropGeneFuncProtoImmortalJellyfish(_json), 
			"CropGeneFuncProtoConiferLeaf" => new CropGeneFuncProtoConiferLeaf(_json), 
			"CropGeneFuncProtoAntiAcidRain" => new CropGeneFuncProtoAntiAcidRain(_json), 
			"CropGeneFuncProtoCharge" => new CropGeneFuncProtoCharge(_json), 
			"CropGeneFuncProtoFertilityEnhance" => new CropGeneFuncProtoFertilityEnhance(_json), 
			"CropGeneFuncProtoSporeSpray" => new CropGeneFuncProtoSporeSpray(_json), 
			"CropGeneFuncProtoTimeGift" => new CropGeneFuncProtoTimeGift(_json), 
			"CropGeneFuncProtoNutritionEnrich" => new CropGeneFuncProtoNutritionEnrich(_json), 
			"CropGeneFuncProtoFlorescenceExtend" => new CropGeneFuncProtoFlorescenceExtend(_json), 
			"CropGeneFuncProtoGrowUnchecked" => new CropGeneFuncProtoGrowUnchecked(_json), 
			"CropGeneFuncProtoBonsai" => new CropGeneFuncProtoBonsai(_json), 
			"CropGeneFuncProtoFirefly" => new CropGeneFuncProtoFirefly(_json), 
			"CropGeneFuncProtoHope" => new CropGeneFuncProtoHope(_json), 
			"CropGeneFuncProtoHanabi" => new CropGeneFuncProtoHanabi(_json), 
			"CropGeneFuncProtoRobustHealth" => new CropGeneFuncProtoRobustHealth(_json), 
			"CropGeneFuncProtoInfertility" => new CropGeneFuncProtoInfertility(_json), 
			"CropGeneFuncProtoAerialRoot" => new CropGeneFuncProtoAerialRoot(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
