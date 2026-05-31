using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemSubTypeInfo : BeanBase
{
	public const int __ID__ = -1907986566;

	public string Id { get; private set; }

	public string MainType { get; private set; }

	public ItemMainTypeInfo MainType_Ref { get; private set; }

	public string AutomationType { get; private set; }

	public ItemAutomationTypeInfo AutomationType_Ref { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public ItemSubTypeInfo(JSONNode _json)
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
		if (!_json["automation_type"].IsString)
		{
			throw new SerializationException();
		}
		AutomationType = _json["automation_type"];
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
	}

	public ItemSubTypeInfo(string id, string main_type, string automation_type, string title)
	{
		Id = id;
		MainType = main_type;
		AutomationType = automation_type;
		Title = title;
	}

	public static ItemSubTypeInfo DeserializeItemSubTypeInfo(JSONNode _json)
	{
		return new ItemSubTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1907986566;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MainType_Ref = (_tables["Item.TbItemMainType"] as TbItemMainType).GetOrDefault(MainType);
		AutomationType_Ref = (_tables["Item.TbItemAutomationType"] as TbItemAutomationType).GetOrDefault(AutomationType);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MainType:" + MainType + ",AutomationType:" + AutomationType + ",Title:" + Title + ",}";
	}
}
