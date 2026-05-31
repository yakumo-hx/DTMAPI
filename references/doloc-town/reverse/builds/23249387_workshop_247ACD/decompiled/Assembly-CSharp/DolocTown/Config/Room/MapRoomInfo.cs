using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class MapRoomInfo : BeanBase
{
	public const int __ID__ = 1170937106;

	public string Id { get; private set; }

	public string MapRoomType { get; private set; }

	public MapRoomTypeInfo MapRoomType_Ref { get; private set; }

	public bool Visible { get; private set; }

	public Vector2Int RoomOffset { get; private set; }

	public Vector2Int SizeOffset { get; private set; }

	public MapRoomInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["map_room_type"].IsString)
		{
			throw new SerializationException();
		}
		MapRoomType = _json["map_room_type"];
		if (!_json["visible"].IsBoolean)
		{
			throw new SerializationException();
		}
		Visible = _json["visible"];
		if (!_json["room_offset"].IsObject)
		{
			throw new SerializationException();
		}
		RoomOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["room_offset"]));
		if (!_json["size_offset"].IsObject)
		{
			throw new SerializationException();
		}
		SizeOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["size_offset"]));
	}

	public MapRoomInfo(string id, string map_room_type, bool visible, Vector2Int room_offset, Vector2Int size_offset)
	{
		Id = id;
		MapRoomType = map_room_type;
		Visible = visible;
		RoomOffset = room_offset;
		SizeOffset = size_offset;
	}

	public static MapRoomInfo DeserializeMapRoomInfo(JSONNode _json)
	{
		return new MapRoomInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1170937106;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MapRoomType_Ref = (_tables["Room.TbMapRoomType"] as TbMapRoomType).GetOrDefault(MapRoomType);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MapRoomType:" + MapRoomType + ",Visible:" + Visible + ",RoomOffset:" + RoomOffset.ToString() + ",SizeOffset:" + SizeOffset.ToString() + ",}";
	}
}
