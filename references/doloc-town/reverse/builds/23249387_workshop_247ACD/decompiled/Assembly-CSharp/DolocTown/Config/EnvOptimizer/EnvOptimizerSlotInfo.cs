using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.EnvOptimizer;

public sealed class EnvOptimizerSlotInfo : BeanBase
{
	public const int __ID__ = -1259434470;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public EnvOptimizerComponentType ComponentType { get; private set; }

	public int RequirePower { get; private set; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public EnvOptimizerSlotInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["component_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ComponentType = (EnvOptimizerComponentType)_json["component_type"].AsInt;
		if (!_json["require_power"].IsNumber)
		{
			throw new SerializationException();
		}
		RequirePower = _json["require_power"];
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
	}

	public EnvOptimizerSlotInfo(string id, EnvOptimizerComponentType component_type, int require_power, string description)
	{
		Id = id;
		ComponentType = component_type;
		RequirePower = require_power;
		Description = description;
	}

	public static EnvOptimizerSlotInfo DeserializeEnvOptimizerSlotInfo(JSONNode _json)
	{
		return new EnvOptimizerSlotInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1259434470;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Description = translator(Description_l10n_key, Description);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ComponentType:" + ComponentType.ToString() + ",RequirePower:" + RequirePower + ",Description:" + Description + ",}";
	}
}
