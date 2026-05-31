using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionHatShield : ItemFunctionHatBase
{
	public const int __ID__ = 2021030270;

	public int MaxShieldValue { get; private set; }

	public ItemFunctionHatShield(JSONNode _json)
		: base(_json)
	{
		if (!_json["max_shield_value"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxShieldValue = _json["max_shield_value"];
	}

	public ItemFunctionHatShield(string hat_id, int max_shield_value)
		: base(hat_id)
	{
		MaxShieldValue = max_shield_value;
	}

	public static ItemFunctionHatShield DeserializeItemFunctionHatShield(JSONNode _json)
	{
		return new ItemFunctionHatShield(_json);
	}

	public override int GetTypeId()
	{
		return 2021030270;
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
		return "{ HatId:" + base.HatId + ",MaxShieldValue:" + MaxShieldValue + ",}";
	}
}
