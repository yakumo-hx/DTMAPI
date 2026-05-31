using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Recipe;

public sealed class RecipeSubTypeInfo : BeanBase
{
	public const int __ID__ = 543252122;

	public string Id { get; private set; }

	public string MainType { get; private set; }

	public RecipeMainTypeInfo MainType_Ref { get; private set; }

	public bool Cookable { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string ItemTitleFormat { get; private set; }

	public string ItemTitleFormat_l10n_key { get; }

	public string ItemDescFormat { get; private set; }

	public string ItemDescFormat_l10n_key { get; }

	public RecipeSubTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["main_type"].IsString)
		{
			throw new SerializationException();
		}
		MainType = _json["main_type"];
		if (!_json["cookable"].IsBoolean)
		{
			throw new SerializationException();
		}
		Cookable = _json["cookable"];
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
		if (!_json["item_title_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemTitleFormat_l10n_key = _json["item_title_format"]["key"];
		if (!_json["item_title_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemTitleFormat = _json["item_title_format"]["text"];
		if (!_json["item_desc_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		ItemDescFormat_l10n_key = _json["item_desc_format"]["key"];
		if (!_json["item_desc_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		ItemDescFormat = _json["item_desc_format"]["text"];
	}

	public RecipeSubTypeInfo(string id, string main_type, bool cookable, string title, string item_title_format, string item_desc_format)
	{
		Id = id;
		MainType = main_type;
		Cookable = cookable;
		Title = title;
		ItemTitleFormat = item_title_format;
		ItemDescFormat = item_desc_format;
	}

	public static RecipeSubTypeInfo DeserializeRecipeSubTypeInfo(JSONNode _json)
	{
		return new RecipeSubTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 543252122;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MainType_Ref = (_tables["Recipe.TbRecipeMainType"] as TbRecipeMainType).GetOrDefault(MainType);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		ItemTitleFormat = translator(ItemTitleFormat_l10n_key, ItemTitleFormat);
		ItemDescFormat = translator(ItemDescFormat_l10n_key, ItemDescFormat);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MainType:" + MainType + ",Cookable:" + Cookable + ",Title:" + Title + ",ItemTitleFormat:" + ItemTitleFormat + ",ItemDescFormat:" + ItemDescFormat + ",}";
	}
}
