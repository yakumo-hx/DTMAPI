using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Tile;
using SimpleJSON;

namespace DolocTown.Config.Sound;

public sealed class MaterialSoundInfo : BeanBase
{
	public const int __ID__ = -959507049;

	public TileMaterial Id { get; private set; }

	public string GroupName { get; private set; }

	public string StateName { get; private set; }

	public MaterialSoundInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (TileMaterial)_json["id"].AsInt;
		if (!_json["groupName"].IsString)
		{
			throw new SerializationException();
		}
		GroupName = _json["groupName"];
		if (!_json["stateName"].IsString)
		{
			throw new SerializationException();
		}
		StateName = _json["stateName"];
	}

	public MaterialSoundInfo(TileMaterial id, string groupName, string stateName)
	{
		Id = id;
		GroupName = groupName;
		StateName = stateName;
	}

	public static MaterialSoundInfo DeserializeMaterialSoundInfo(JSONNode _json)
	{
		return new MaterialSoundInfo(_json);
	}

	public override int GetTypeId()
	{
		return -959507049;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",GroupName:" + GroupName + ",StateName:" + StateName + ",}";
	}
}
