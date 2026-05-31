using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoExtraResource : AgentEquipmentFuncProto
{
	public const int __ID__ = 1567257456;

	public string[] ResourceIds { get; private set; }

	public float Probability { get; private set; }

	public string ItemLut { get; private set; }

	public Vector2Int Range { get; private set; }

	public AgentEquipmentFuncProtoExtraResource(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["resource_ids"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ResourceIds = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ResourceIds[num++] = text;
		}
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
		if (!_json["item_lut"].IsString)
		{
			throw new SerializationException();
		}
		ItemLut = _json["item_lut"];
		if (!_json["range"].IsObject)
		{
			throw new SerializationException();
		}
		Range = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["range"]));
	}

	public AgentEquipmentFuncProtoExtraResource(string[] resource_ids, float probability, string item_lut, Vector2Int range)
	{
		ResourceIds = resource_ids;
		Probability = probability;
		ItemLut = item_lut;
		Range = range;
	}

	public static AgentEquipmentFuncProtoExtraResource DeserializeAgentEquipmentFuncProtoExtraResource(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoExtraResource(_json);
	}

	public override int GetTypeId()
	{
		return 1567257456;
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
		return "{ ResourceIds:" + StringUtil.CollectionToString(ResourceIds) + ",Probability:" + Probability + ",ItemLut:" + ItemLut + ",Range:" + Range.ToString() + ",}";
	}
}
