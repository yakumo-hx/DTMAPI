using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class MapRoomTypeInfo : BeanBase
{
	public const int __ID__ = 1584273772;

	public string Id { get; private set; }

	public Color BorderColor { get; private set; }

	public Color TerrainColor { get; private set; }

	public Color FogColor { get; private set; }

	public MapRoomTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["border_color"].IsObject)
		{
			throw new SerializationException();
		}
		BorderColor = ExternalTypeUtil.ColorConverter(CfgColor.DeserializeCfgColor(_json["border_color"]));
		if (!_json["terrain_color"].IsObject)
		{
			throw new SerializationException();
		}
		TerrainColor = ExternalTypeUtil.ColorConverter(CfgColor.DeserializeCfgColor(_json["terrain_color"]));
		if (!_json["fog_color"].IsObject)
		{
			throw new SerializationException();
		}
		FogColor = ExternalTypeUtil.ColorConverter(CfgColor.DeserializeCfgColor(_json["fog_color"]));
	}

	public MapRoomTypeInfo(string id, Color border_color, Color terrain_color, Color fog_color)
	{
		Id = id;
		BorderColor = border_color;
		TerrainColor = terrain_color;
		FogColor = fog_color;
	}

	public static MapRoomTypeInfo DeserializeMapRoomTypeInfo(JSONNode _json)
	{
		return new MapRoomTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1584273772;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",BorderColor:" + BorderColor.ToString() + ",TerrainColor:" + TerrainColor.ToString() + ",FogColor:" + FogColor.ToString() + ",}";
	}
}
