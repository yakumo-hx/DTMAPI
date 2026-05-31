using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class BackgroundHighLevelInfo : BeanBase
{
	public const int __ID__ = -864765905;

	public BackgroundHighLevel Id { get; private set; }

	public float Offset { get; private set; }

	public BackgroundHighLevelInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (BackgroundHighLevel)_json["id"].AsInt;
		if (!_json["offset"].IsNumber)
		{
			throw new SerializationException();
		}
		Offset = _json["offset"];
	}

	public BackgroundHighLevelInfo(BackgroundHighLevel id, float offset)
	{
		Id = id;
		Offset = offset;
	}

	public static BackgroundHighLevelInfo DeserializeBackgroundHighLevelInfo(JSONNode _json)
	{
		return new BackgroundHighLevelInfo(_json);
	}

	public override int GetTypeId()
	{
		return -864765905;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",Offset:" + Offset + ",}";
	}
}
