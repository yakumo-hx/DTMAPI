using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class MapAreaInfo : BeanBase
{
	public const int __ID__ = -321897532;

	public string MapId { get; private set; }

	public MapTypeInfo MapId_Ref { get; private set; }

	public string AreaId { get; private set; }

	public string Comment { get; private set; }

	public MapAreaType MapAreaType { get; private set; }

	public Vector2Int AreaOffset { get; private set; }

	public Vector2Int AreaSize { get; private set; }

	public string RoomName { get; private set; }

	public Padding Padding { get; private set; }

	public float FogMaxRate { get; private set; }

	public bool NeedUnlock { get; private set; }

	public string[] CoveredRooms { get; private set; }

	public string Label { get; private set; }

	public string Label_l10n_key { get; }

	public Vector2Int AreaOffsetWithPadding { get; private set; }

	public Vector2Int AreaSizeWithPadding { get; private set; }

	public MapAreaInfo(JSONNode _json)
	{
		if (!_json["map_id"].IsString)
		{
			throw new SerializationException();
		}
		MapId = _json["map_id"];
		if (!_json["area_id"].IsString)
		{
			throw new SerializationException();
		}
		AreaId = _json["area_id"];
		if (!_json["comment"].IsString)
		{
			throw new SerializationException();
		}
		Comment = _json["comment"];
		if (!_json["map_area_type"].IsNumber)
		{
			throw new SerializationException();
		}
		MapAreaType = (MapAreaType)_json["map_area_type"].AsInt;
		if (!_json["area_offset"].IsObject)
		{
			throw new SerializationException();
		}
		AreaOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["area_offset"]));
		if (!_json["area_size"].IsObject)
		{
			throw new SerializationException();
		}
		AreaSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["area_size"]));
		if (!_json["room_name"].IsString)
		{
			throw new SerializationException();
		}
		RoomName = _json["room_name"];
		if (!_json["padding"].IsObject)
		{
			throw new SerializationException();
		}
		Padding = Padding.DeserializePadding(_json["padding"]);
		if (!_json["fog_max_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		FogMaxRate = _json["fog_max_rate"];
		if (!_json["need_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		NeedUnlock = _json["need_unlock"];
		JSONNode jSONNode = _json["covered_rooms"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		CoveredRooms = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			CoveredRooms[num++] = text;
		}
		if (!_json["label"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Label_l10n_key = _json["label"]["key"];
		if (!_json["label"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Label = _json["label"]["text"];
	}

	public MapAreaInfo(string map_id, string area_id, string comment, MapAreaType map_area_type, Vector2Int area_offset, Vector2Int area_size, string room_name, Padding padding, float fog_max_rate, bool need_unlock, string[] covered_rooms, string label)
	{
		MapId = map_id;
		AreaId = area_id;
		Comment = comment;
		MapAreaType = map_area_type;
		AreaOffset = area_offset;
		AreaSize = area_size;
		RoomName = room_name;
		Padding = padding;
		FogMaxRate = fog_max_rate;
		NeedUnlock = need_unlock;
		CoveredRooms = covered_rooms;
		Label = label;
	}

	public static MapAreaInfo DeserializeMapAreaInfo(JSONNode _json)
	{
		return new MapAreaInfo(_json);
	}

	public override int GetTypeId()
	{
		return -321897532;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		MapId_Ref = (_tables["Room.TbMapType"] as TbMapType).GetOrDefault(MapId);
		Padding?.Resolve(_tables);
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Padding?.TranslateText(translator);
		Label = translator(Label_l10n_key, Label);
	}

	public override string ToString()
	{
		return "{ MapId:" + MapId + ",AreaId:" + AreaId + ",Comment:" + Comment + ",MapAreaType:" + MapAreaType.ToString() + ",AreaOffset:" + AreaOffset.ToString() + ",AreaSize:" + AreaSize.ToString() + ",RoomName:" + RoomName + ",Padding:" + Padding?.ToString() + ",FogMaxRate:" + FogMaxRate + ",NeedUnlock:" + NeedUnlock + ",CoveredRooms:" + StringUtil.CollectionToString(CoveredRooms) + ",Label:" + Label + ",}";
	}

	private void PostResolve()
	{
		AreaOffsetWithPadding = new Vector2Int(AreaOffset.x - Padding.L, AreaOffset.y - Padding.B);
		AreaSizeWithPadding = new Vector2Int(AreaSize.x + Padding.L + Padding.R, AreaSize.y + Padding.B + Padding.T);
	}
}
