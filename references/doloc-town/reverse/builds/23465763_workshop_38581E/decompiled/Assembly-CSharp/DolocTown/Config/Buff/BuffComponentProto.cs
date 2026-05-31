using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Buff;

public sealed class BuffComponentProto : BeanBase
{
	public const int __ID__ = 769406873;

	public BuffBasicType BuffType { get; private set; }

	public float Value { get; private set; }

	public BuffComponentProto(JSONNode _json)
	{
		if (!_json["buff_type"].IsNumber)
		{
			throw new SerializationException();
		}
		BuffType = (BuffBasicType)_json["buff_type"].AsInt;
		if (!_json["value"].IsNumber)
		{
			throw new SerializationException();
		}
		Value = _json["value"];
	}

	public BuffComponentProto(BuffBasicType buff_type, float value)
	{
		BuffType = buff_type;
		Value = value;
	}

	public static BuffComponentProto DeserializeBuffComponentProto(JSONNode _json)
	{
		return new BuffComponentProto(_json);
	}

	public override int GetTypeId()
	{
		return 769406873;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ BuffType:" + BuffType.ToString() + ",Value:" + Value + ",}";
	}
}
