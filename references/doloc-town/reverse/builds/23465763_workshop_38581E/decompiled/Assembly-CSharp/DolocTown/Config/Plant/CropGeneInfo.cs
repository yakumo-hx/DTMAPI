using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneInfo : BeanBase
{
	public const int __ID__ = 1218844406;

	public string Id { get; private set; }

	public string CapsuleItem { get; private set; }

	public ItemInfo CapsuleItem_Ref { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public int CompressInheritWeight { get; private set; }

	public int SynthesizeInheritWeight { get; private set; }

	public CropGeneFuncProto Function { get; private set; }

	public CropGeneInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["capsule_item"].IsString)
		{
			throw new SerializationException();
		}
		CapsuleItem = _json["capsule_item"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["compress_inherit_weight"].IsNumber)
		{
			throw new SerializationException();
		}
		CompressInheritWeight = _json["compress_inherit_weight"];
		if (!_json["synthesize_inherit_weight"].IsNumber)
		{
			throw new SerializationException();
		}
		SynthesizeInheritWeight = _json["synthesize_inherit_weight"];
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = CropGeneFuncProto.DeserializeCropGeneFuncProto(_json["function"]);
	}

	public CropGeneInfo(string id, string capsule_item, bool default_unlock, string title, string description, int compress_inherit_weight, int synthesize_inherit_weight, CropGeneFuncProto function)
	{
		Id = id;
		CapsuleItem = capsule_item;
		DefaultUnlock = default_unlock;
		Title = title;
		Description = description;
		CompressInheritWeight = compress_inherit_weight;
		SynthesizeInheritWeight = synthesize_inherit_weight;
		Function = function;
	}

	public static CropGeneInfo DeserializeCropGeneInfo(JSONNode _json)
	{
		return new CropGeneInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1218844406;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		CapsuleItem_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(CapsuleItem);
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",CapsuleItem:" + CapsuleItem + ",DefaultUnlock:" + DefaultUnlock + ",Title:" + Title + ",Description:" + Description + ",CompressInheritWeight:" + CompressInheritWeight + ",SynthesizeInheritWeight:" + SynthesizeInheritWeight + ",Function:" + Function?.ToString() + ",}";
	}
}
