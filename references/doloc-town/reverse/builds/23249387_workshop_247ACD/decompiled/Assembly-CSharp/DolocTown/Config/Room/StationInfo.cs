using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class StationInfo : BeanBase
{
	public const int __ID__ = -1020734609;

	public string Id { get; private set; }

	public bool FreeTeleport { get; private set; }

	public string MarkPointId { get; private set; }

	public MarkPointInfo MarkPointId_Ref { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public Vector2Int MapPosition { get; private set; }

	public StationInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["free_teleport"].IsBoolean)
		{
			throw new SerializationException();
		}
		FreeTeleport = _json["free_teleport"];
		if (!_json["mark_point_id"].IsString)
		{
			throw new SerializationException();
		}
		MarkPointId = _json["mark_point_id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["map_position"].IsObject)
		{
			throw new SerializationException();
		}
		MapPosition = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["map_position"]));
	}

	public StationInfo(string id, bool free_teleport, string mark_point_id, string title, Vector2Int map_position)
	{
		Id = id;
		FreeTeleport = free_teleport;
		MarkPointId = mark_point_id;
		Title = title;
		MapPosition = map_position;
	}

	public static StationInfo DeserializeStationInfo(JSONNode _json)
	{
		return new StationInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1020734609;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MarkPointId_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(MarkPointId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",FreeTeleport:" + FreeTeleport + ",MarkPointId:" + MarkPointId + ",Title:" + Title + ",MapPosition:" + MapPosition.ToString() + ",}";
	}
}
