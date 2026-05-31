using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneFuncProtoFertilityEnhance : CropGeneFuncProto
{
	public const int __ID__ = 1729111456;

	public string FertilizerItem { get; private set; }

	public ItemInfo FertilizerItem_Ref { get; private set; }

	public CropGeneFuncProtoFertilityEnhance(JSONNode _json)
		: base(_json)
	{
		if (!_json["fertilizer_item"].IsString)
		{
			throw new SerializationException();
		}
		FertilizerItem = _json["fertilizer_item"];
	}

	public CropGeneFuncProtoFertilityEnhance(string fertilizer_item)
	{
		FertilizerItem = fertilizer_item;
	}

	public static CropGeneFuncProtoFertilityEnhance DeserializeCropGeneFuncProtoFertilityEnhance(JSONNode _json)
	{
		return new CropGeneFuncProtoFertilityEnhance(_json);
	}

	public override int GetTypeId()
	{
		return 1729111456;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		FertilizerItem_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(FertilizerItem);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ FertilizerItem:" + FertilizerItem + ",}";
	}
}
