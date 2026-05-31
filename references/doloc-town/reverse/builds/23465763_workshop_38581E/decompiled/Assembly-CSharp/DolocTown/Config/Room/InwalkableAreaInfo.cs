using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class InwalkableAreaInfo : BeanBase
{
	public const int __ID__ = -755895882;

	public string Id { get; private set; }

	public string SceneName { get; private set; }

	public float Left { get; private set; }

	public float Right { get; private set; }

	public InwalkableAreaInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["scene_name"].IsString)
		{
			throw new SerializationException();
		}
		SceneName = _json["scene_name"];
		if (!_json["left"].IsNumber)
		{
			throw new SerializationException();
		}
		Left = _json["left"];
		if (!_json["right"].IsNumber)
		{
			throw new SerializationException();
		}
		Right = _json["right"];
	}

	public InwalkableAreaInfo(string id, string scene_name, float left, float right)
	{
		Id = id;
		SceneName = scene_name;
		Left = left;
		Right = right;
	}

	public static InwalkableAreaInfo DeserializeInwalkableAreaInfo(JSONNode _json)
	{
		return new InwalkableAreaInfo(_json);
	}

	public override int GetTypeId()
	{
		return -755895882;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SceneName:" + SceneName + ",Left:" + Left + ",Right:" + Right + ",}";
	}
}
