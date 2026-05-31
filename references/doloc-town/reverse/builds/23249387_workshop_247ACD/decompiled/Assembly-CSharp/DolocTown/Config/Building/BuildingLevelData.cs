using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Room;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Building;

public sealed class BuildingLevelData : BeanBase
{
	public const int __ID__ = -1853407168;

	public string RoomEffect { get; private set; }

	public RoomEffectInfo RoomEffect_Ref { get; private set; }

	public string TemplateRoomName { get; private set; }

	public Vector2[] LinkGatesLeft { get; private set; }

	public Vector2[] LinkGatesRight { get; private set; }

	public Vector2[] LinkGatesBottom { get; private set; }

	public Vector2[] LinkGatesTop { get; private set; }

	public BuildingLevelData(JSONNode _json)
	{
		if (!_json["room_effect"].IsString)
		{
			throw new SerializationException();
		}
		RoomEffect = _json["room_effect"];
		if (!_json["template_room_name"].IsString)
		{
			throw new SerializationException();
		}
		TemplateRoomName = _json["template_room_name"];
		JSONNode jSONNode = _json["link_gates_left"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		LinkGatesLeft = new Vector2[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			Vector2 vector = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(child));
			LinkGatesLeft[num++] = vector;
		}
		JSONNode jSONNode2 = _json["link_gates_right"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		LinkGatesRight = new Vector2[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			Vector2 vector2 = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(child2));
			LinkGatesRight[num2++] = vector2;
		}
		JSONNode jSONNode3 = _json["link_gates_bottom"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		LinkGatesBottom = new Vector2[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			Vector2 vector3 = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(child3));
			LinkGatesBottom[num3++] = vector3;
		}
		JSONNode jSONNode4 = _json["link_gates_top"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		LinkGatesTop = new Vector2[count4];
		int num4 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			Vector2 vector4 = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(child4));
			LinkGatesTop[num4++] = vector4;
		}
	}

	public BuildingLevelData(string room_effect, string template_room_name, Vector2[] link_gates_left, Vector2[] link_gates_right, Vector2[] link_gates_bottom, Vector2[] link_gates_top)
	{
		RoomEffect = room_effect;
		TemplateRoomName = template_room_name;
		LinkGatesLeft = link_gates_left;
		LinkGatesRight = link_gates_right;
		LinkGatesBottom = link_gates_bottom;
		LinkGatesTop = link_gates_top;
	}

	public static BuildingLevelData DeserializeBuildingLevelData(JSONNode _json)
	{
		return new BuildingLevelData(_json);
	}

	public override int GetTypeId()
	{
		return -1853407168;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		RoomEffect_Ref = (_tables["Room.TbRoomEffect"] as TbRoomEffect).GetOrDefault(RoomEffect);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ RoomEffect:" + RoomEffect + ",TemplateRoomName:" + TemplateRoomName + ",LinkGatesLeft:" + StringUtil.CollectionToString(LinkGatesLeft) + ",LinkGatesRight:" + StringUtil.CollectionToString(LinkGatesRight) + ",LinkGatesBottom:" + StringUtil.CollectionToString(LinkGatesBottom) + ",LinkGatesTop:" + StringUtil.CollectionToString(LinkGatesTop) + ",}";
	}
}
