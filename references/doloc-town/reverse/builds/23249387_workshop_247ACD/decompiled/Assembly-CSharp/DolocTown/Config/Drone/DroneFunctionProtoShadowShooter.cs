using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoShadowShooter : DroneFunctionProto
{
	public const int __ID__ = 825603806;

	public float Probability { get; private set; }

	public string BulletId { get; private set; }

	public DroneFunctionProtoShadowShooter(JSONNode _json)
		: base(_json)
	{
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
		if (!_json["bullet_id"].IsString)
		{
			throw new SerializationException();
		}
		BulletId = _json["bullet_id"];
	}

	public DroneFunctionProtoShadowShooter(float probability, string bullet_id)
	{
		Probability = probability;
		BulletId = bullet_id;
	}

	public static DroneFunctionProtoShadowShooter DeserializeDroneFunctionProtoShadowShooter(JSONNode _json)
	{
		return new DroneFunctionProtoShadowShooter(_json);
	}

	public override int GetTypeId()
	{
		return 825603806;
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
		return "{ Probability:" + Probability + ",BulletId:" + BulletId + ",}";
	}
}
