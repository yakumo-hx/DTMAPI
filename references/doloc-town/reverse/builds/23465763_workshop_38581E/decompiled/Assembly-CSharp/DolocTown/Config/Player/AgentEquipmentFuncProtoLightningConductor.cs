using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoLightningConductor : AgentEquipmentFuncProto
{
	public const int __ID__ = -1410388341;

	public int CdDuration { get; private set; }

	public float Damage { get; private set; }

	public float Range { get; private set; }

	public AgentEquipmentFuncProtoLightningConductor(JSONNode _json)
		: base(_json)
	{
		if (!_json["cd_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		CdDuration = _json["cd_duration"];
		if (!_json["damage"].IsNumber)
		{
			throw new SerializationException();
		}
		Damage = _json["damage"];
		if (!_json["range"].IsNumber)
		{
			throw new SerializationException();
		}
		Range = _json["range"];
	}

	public AgentEquipmentFuncProtoLightningConductor(int cd_duration, float damage, float range)
	{
		CdDuration = cd_duration;
		Damage = damage;
		Range = range;
	}

	public static AgentEquipmentFuncProtoLightningConductor DeserializeAgentEquipmentFuncProtoLightningConductor(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoLightningConductor(_json);
	}

	public override int GetTypeId()
	{
		return -1410388341;
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
		return "{ CdDuration:" + CdDuration + ",Damage:" + Damage + ",Range:" + Range + ",}";
	}
}
