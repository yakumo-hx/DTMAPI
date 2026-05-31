using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class MarkPointInfo : BeanBase
{
	public const int __ID__ = 2077745726;

	public string Id { get; private set; }

	public string RoomId { get; private set; }

	public Vector2 Position { get; private set; }

	public string SceneRawName => RoomId.Split(".")[0];

	public SceneType RoomType => RoomId switch
	{
		"farm_" => SceneType.FARM, 
		"dungeon_" => SceneType.DUNGEON, 
		"city_" => SceneType.CITY, 
		_ => SceneType.NONE, 
	};

	public MarkPointInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["room_id"].IsString)
		{
			throw new SerializationException();
		}
		RoomId = _json["room_id"];
		if (!_json["position"].IsObject)
		{
			throw new SerializationException();
		}
		Position = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["position"]));
	}

	public MarkPointInfo(string id, string room_id, Vector2 position)
	{
		Id = id;
		RoomId = room_id;
		Position = position;
	}

	public static MarkPointInfo DeserializeMarkPointInfo(JSONNode _json)
	{
		return new MarkPointInfo(_json);
	}

	public override int GetTypeId()
	{
		return 2077745726;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",RoomId:" + RoomId + ",Position:" + Position.ToString() + ",}";
	}
}
