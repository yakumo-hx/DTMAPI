using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Tile;
using SimpleJSON;

namespace DolocTown.Config.Sound;

public sealed class FootStepInfo : BeanBase
{
	public const int __ID__ = -1666867481;

	public string Id { get; private set; }

	public TileMaterial TileMaterial { get; private set; }

	public FootStepInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["tileMaterial"].IsNumber)
		{
			throw new SerializationException();
		}
		TileMaterial = (TileMaterial)_json["tileMaterial"].AsInt;
	}

	public FootStepInfo(string id, TileMaterial tileMaterial)
	{
		Id = id;
		TileMaterial = tileMaterial;
	}

	public static FootStepInfo DeserializeFootStepInfo(JSONNode _json)
	{
		return new FootStepInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1666867481;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",TileMaterial:" + TileMaterial.ToString() + ",}";
	}
}
