using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class MapTypeInfo : BeanBase
{
	public const int __ID__ = 667933681;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public Vector2Int MapSize { get; private set; }

	public Vector2Int MapOffset { get; private set; }

	public bool UseMask { get; private set; }

	public Vector2Int CameraSize { get; private set; }

	public string MiniMap { get; private set; }

	public string MainMap { get; private set; }

	public SpriteAsset PointerIcon { get; private set; }

	public SpriteAsset CursorIcon { get; private set; }

	public MapTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
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
		if (!_json["map_size"].IsObject)
		{
			throw new SerializationException();
		}
		MapSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["map_size"]));
		if (!_json["map_offset"].IsObject)
		{
			throw new SerializationException();
		}
		MapOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["map_offset"]));
		if (!_json["use_mask"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseMask = _json["use_mask"];
		if (!_json["camera_size"].IsObject)
		{
			throw new SerializationException();
		}
		CameraSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["camera_size"]));
		if (!_json["mini_map"].IsString)
		{
			throw new SerializationException();
		}
		MiniMap = _json["mini_map"];
		if (!_json["main_map"].IsString)
		{
			throw new SerializationException();
		}
		MainMap = _json["main_map"];
		if (!_json["pointer_icon"].IsObject)
		{
			throw new SerializationException();
		}
		PointerIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["pointer_icon"]));
		if (!_json["cursor_icon"].IsObject)
		{
			throw new SerializationException();
		}
		CursorIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["cursor_icon"]));
	}

	public MapTypeInfo(string id, string title, Vector2Int map_size, Vector2Int map_offset, bool use_mask, Vector2Int camera_size, string mini_map, string main_map, SpriteAsset pointer_icon, SpriteAsset cursor_icon)
	{
		Id = id;
		Title = title;
		MapSize = map_size;
		MapOffset = map_offset;
		UseMask = use_mask;
		CameraSize = camera_size;
		MiniMap = mini_map;
		MainMap = main_map;
		PointerIcon = pointer_icon;
		CursorIcon = cursor_icon;
	}

	public static MapTypeInfo DeserializeMapTypeInfo(JSONNode _json)
	{
		return new MapTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 667933681;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",MapSize:" + MapSize.ToString() + ",MapOffset:" + MapOffset.ToString() + ",UseMask:" + UseMask + ",CameraSize:" + CameraSize.ToString() + ",MiniMap:" + MiniMap + ",MainMap:" + MainMap + ",PointerIcon:" + PointerIcon?.ToString() + ",CursorIcon:" + CursorIcon?.ToString() + ",}";
	}
}
