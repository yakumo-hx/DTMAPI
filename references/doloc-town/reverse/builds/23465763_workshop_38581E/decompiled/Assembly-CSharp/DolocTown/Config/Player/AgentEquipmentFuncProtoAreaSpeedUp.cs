using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoAreaSpeedUp : AgentEquipmentFuncProto
{
	public const int __ID__ = -1968799833;

	public string[] RoomNames { get; private set; }

	public float SpeedUp { get; private set; }

	public AgentEquipmentFuncProtoAreaSpeedUp(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["room_names"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		RoomNames = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			RoomNames[num++] = text;
		}
		if (!_json["speed_up"].IsNumber)
		{
			throw new SerializationException();
		}
		SpeedUp = _json["speed_up"];
	}

	public AgentEquipmentFuncProtoAreaSpeedUp(string[] room_names, float speed_up)
	{
		RoomNames = room_names;
		SpeedUp = speed_up;
	}

	public static AgentEquipmentFuncProtoAreaSpeedUp DeserializeAgentEquipmentFuncProtoAreaSpeedUp(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoAreaSpeedUp(_json);
	}

	public override int GetTypeId()
	{
		return -1968799833;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ RoomNames:" + StringUtil.CollectionToString(RoomNames) + ",SpeedUp:" + SpeedUp + ",}";
	}
}
