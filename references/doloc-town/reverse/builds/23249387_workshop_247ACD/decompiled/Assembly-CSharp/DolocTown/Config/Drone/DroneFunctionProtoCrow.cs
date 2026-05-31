using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoCrow : DroneFunctionProto
{
	public const int __ID__ = -1115814075;

	public float PowerRecv { get; private set; }

	public DroneFunctionProtoCrow(JSONNode _json)
		: base(_json)
	{
		if (!_json["power_recv"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerRecv = _json["power_recv"];
	}

	public DroneFunctionProtoCrow(float power_recv)
	{
		PowerRecv = power_recv;
	}

	public static DroneFunctionProtoCrow DeserializeDroneFunctionProtoCrow(JSONNode _json)
	{
		return new DroneFunctionProtoCrow(_json);
	}

	public override int GetTypeId()
	{
		return -1115814075;
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
		return "{ PowerRecv:" + PowerRecv + ",}";
	}
}
