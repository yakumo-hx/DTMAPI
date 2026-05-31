using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoBrustCore : DroneFunctionProto
{
	public const int __ID__ = 297605143;

	public int BulletInterval { get; private set; }

	public int BulletCount { get; private set; }

	public DroneFunctionProtoBrustCore(JSONNode _json)
		: base(_json)
	{
		if (!_json["bullet_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		BulletInterval = _json["bullet_interval"];
		if (!_json["bullet_count"].IsNumber)
		{
			throw new SerializationException();
		}
		BulletCount = _json["bullet_count"];
	}

	public DroneFunctionProtoBrustCore(int bullet_interval, int bullet_count)
	{
		BulletInterval = bullet_interval;
		BulletCount = bullet_count;
	}

	public static DroneFunctionProtoBrustCore DeserializeDroneFunctionProtoBrustCore(JSONNode _json)
	{
		return new DroneFunctionProtoBrustCore(_json);
	}

	public override int GetTypeId()
	{
		return 297605143;
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
		return "{ BulletInterval:" + BulletInterval + ",BulletCount:" + BulletCount + ",}";
	}
}
