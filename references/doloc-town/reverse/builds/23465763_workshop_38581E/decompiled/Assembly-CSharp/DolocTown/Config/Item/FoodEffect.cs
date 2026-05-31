using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Buff;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class FoodEffect : BeanBase
{
	public const int __ID__ = 1599869258;

	public string Buff { get; private set; }

	public BuffInfo Buff_Ref { get; private set; }

	public float Scale { get; private set; }

	public FoodEffect(JSONNode _json)
	{
		if (!_json["buff"].IsString)
		{
			throw new SerializationException();
		}
		Buff = _json["buff"];
		if (!_json["scale"].IsNumber)
		{
			throw new SerializationException();
		}
		Scale = _json["scale"];
	}

	public FoodEffect(string buff, float scale)
	{
		Buff = buff;
		Scale = scale;
	}

	public static FoodEffect DeserializeFoodEffect(JSONNode _json)
	{
		return new FoodEffect(_json);
	}

	public override int GetTypeId()
	{
		return 1599869258;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Buff_Ref = (_tables["Buff.TbBuff"] as TbBuff).GetOrDefault(Buff);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Buff:" + Buff + ",Scale:" + Scale + ",}";
	}
}
