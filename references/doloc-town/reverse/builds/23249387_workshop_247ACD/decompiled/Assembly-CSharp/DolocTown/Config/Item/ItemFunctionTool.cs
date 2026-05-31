using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionTool : ItemFunctionBase
{
	public const int __ID__ = -1823304258;

	public ToolType ToolType { get; private set; }

	public int Level { get; private set; }

	public int ChopNumber { get; private set; }

	public int Attack { get; private set; }

	public int MaxChopObjects { get; private set; }

	public Vector2Int ColliderSize { get; private set; }

	public Vector2Int ColliderOffset { get; private set; }

	public string AgentAnimName { get; private set; }

	public string AudioEvent { get; private set; }

	public ItemFunctionTool(JSONNode _json)
		: base(_json)
	{
		if (!_json["tool_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ToolType = (ToolType)_json["tool_type"].AsInt;
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["chop_number"].IsNumber)
		{
			throw new SerializationException();
		}
		ChopNumber = _json["chop_number"];
		if (!_json["attack"].IsNumber)
		{
			throw new SerializationException();
		}
		Attack = _json["attack"];
		if (!_json["max_chop_objects"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxChopObjects = _json["max_chop_objects"];
		if (!_json["collider_size"].IsObject)
		{
			throw new SerializationException();
		}
		ColliderSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["collider_size"]));
		if (!_json["collider_offset"].IsObject)
		{
			throw new SerializationException();
		}
		ColliderOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["collider_offset"]));
		if (!_json["agent_anim_name"].IsString)
		{
			throw new SerializationException();
		}
		AgentAnimName = _json["agent_anim_name"];
		if (!_json["audio_event"].IsString)
		{
			throw new SerializationException();
		}
		AudioEvent = _json["audio_event"];
	}

	public ItemFunctionTool(ToolType tool_type, int level, int chop_number, int attack, int max_chop_objects, Vector2Int collider_size, Vector2Int collider_offset, string agent_anim_name, string audio_event)
	{
		ToolType = tool_type;
		Level = level;
		ChopNumber = chop_number;
		Attack = attack;
		MaxChopObjects = max_chop_objects;
		ColliderSize = collider_size;
		ColliderOffset = collider_offset;
		AgentAnimName = agent_anim_name;
		AudioEvent = audio_event;
	}

	public static ItemFunctionTool DeserializeItemFunctionTool(JSONNode _json)
	{
		return new ItemFunctionTool(_json);
	}

	public override int GetTypeId()
	{
		return -1823304258;
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
		return "{ ToolType:" + ToolType.ToString() + ",Level:" + Level + ",ChopNumber:" + ChopNumber + ",Attack:" + Attack + ",MaxChopObjects:" + MaxChopObjects + ",ColliderSize:" + ColliderSize.ToString() + ",ColliderOffset:" + ColliderOffset.ToString() + ",AgentAnimName:" + AgentAnimName + ",AudioEvent:" + AudioEvent + ",}";
	}
}
