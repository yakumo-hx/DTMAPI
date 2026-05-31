using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionEdenFruit : ItemFunctionBase
{
	public const int __ID__ = 101140230;

	public int MaxHealth { get; private set; }

	public int MaxEnergy { get; private set; }

	public int MaxSpirit { get; private set; }

	public ItemFunctionEdenFruit(JSONNode _json)
		: base(_json)
	{
		if (!_json["max_health"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxHealth = _json["max_health"];
		if (!_json["max_energy"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxEnergy = _json["max_energy"];
		if (!_json["max_spirit"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxSpirit = _json["max_spirit"];
	}

	public ItemFunctionEdenFruit(int max_health, int max_energy, int max_spirit)
	{
		MaxHealth = max_health;
		MaxEnergy = max_energy;
		MaxSpirit = max_spirit;
	}

	public static ItemFunctionEdenFruit DeserializeItemFunctionEdenFruit(JSONNode _json)
	{
		return new ItemFunctionEdenFruit(_json);
	}

	public override int GetTypeId()
	{
		return 101140230;
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
		return "{ MaxHealth:" + MaxHealth + ",MaxEnergy:" + MaxEnergy + ",MaxSpirit:" + MaxSpirit + ",}";
	}
}
