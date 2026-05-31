using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Animal;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionAnimalPackage : ItemFunctionBase
{
	public const int __ID__ = 97773892;

	public SpriteAsset UiSpriteCatch { get; private set; }

	public string PresetAnimal { get; private set; }

	public AnimalInfo PresetAnimal_Ref { get; private set; }

	public bool Reusable { get; private set; }

	public string TypeWhenFull { get; private set; }

	public ItemSubTypeInfo TypeWhenFull_Ref { get; private set; }

	public ItemFunctionAnimalPackage(JSONNode _json)
		: base(_json)
	{
		if (!_json["ui_sprite_catch"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteCatch = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_catch"]));
		if (!_json["preset_animal"].IsString)
		{
			throw new SerializationException();
		}
		PresetAnimal = _json["preset_animal"];
		if (!_json["reusable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Reusable = _json["reusable"];
		if (!_json["type_when_full"].IsString)
		{
			throw new SerializationException();
		}
		TypeWhenFull = _json["type_when_full"];
	}

	public ItemFunctionAnimalPackage(SpriteAsset ui_sprite_catch, string preset_animal, bool reusable, string type_when_full)
	{
		UiSpriteCatch = ui_sprite_catch;
		PresetAnimal = preset_animal;
		Reusable = reusable;
		TypeWhenFull = type_when_full;
	}

	public static ItemFunctionAnimalPackage DeserializeItemFunctionAnimalPackage(JSONNode _json)
	{
		return new ItemFunctionAnimalPackage(_json);
	}

	public override int GetTypeId()
	{
		return 97773892;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		PresetAnimal_Ref = (_tables["Animal.TbAnimal"] as TbAnimal).GetOrDefault(PresetAnimal);
		TypeWhenFull_Ref = (_tables["Item.TbItemSubType"] as TbItemSubType).GetOrDefault(TypeWhenFull);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ UiSpriteCatch:" + UiSpriteCatch?.ToString() + ",PresetAnimal:" + PresetAnimal + ",Reusable:" + Reusable + ",TypeWhenFull:" + TypeWhenFull + ",}";
	}
}
