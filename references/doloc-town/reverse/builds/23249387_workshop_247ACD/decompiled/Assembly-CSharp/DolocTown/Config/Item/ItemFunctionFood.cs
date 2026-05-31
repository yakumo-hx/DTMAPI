using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFood : ItemFunctionBase
{
	public const int __ID__ = -1823721340;

	public string EatingEffect { get; private set; }

	public EatingEffectInfo EatingEffect_Ref { get; private set; }

	public ItemFunctionFood(JSONNode _json)
		: base(_json)
	{
		if (!_json["eating_effect"].IsString)
		{
			throw new SerializationException();
		}
		EatingEffect = _json["eating_effect"];
	}

	public ItemFunctionFood(string eating_effect)
	{
		EatingEffect = eating_effect;
	}

	public static ItemFunctionFood DeserializeItemFunctionFood(JSONNode _json)
	{
		return new ItemFunctionFood(_json);
	}

	public override int GetTypeId()
	{
		return -1823721340;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		EatingEffect_Ref = (_tables["Item.TbEatingEffect"] as TbEatingEffect).GetOrDefault(EatingEffect);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ EatingEffect:" + EatingEffect + ",}";
	}
}
