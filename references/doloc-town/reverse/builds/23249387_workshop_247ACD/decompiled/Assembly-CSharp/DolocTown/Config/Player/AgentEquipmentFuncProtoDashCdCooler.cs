using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoDashCdCooler : AgentEquipmentFuncProto
{
	public const int __ID__ = 899786167;

	public float CdDecrease { get; private set; }

	public AgentEquipmentFuncProtoDashCdCooler(JSONNode _json)
		: base(_json)
	{
		if (!_json["cd_decrease"].IsNumber)
		{
			throw new SerializationException();
		}
		CdDecrease = _json["cd_decrease"];
	}

	public AgentEquipmentFuncProtoDashCdCooler(float cd_decrease)
	{
		CdDecrease = cd_decrease;
	}

	public static AgentEquipmentFuncProtoDashCdCooler DeserializeAgentEquipmentFuncProtoDashCdCooler(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoDashCdCooler(_json);
	}

	public override int GetTypeId()
	{
		return 899786167;
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
		return "{ CdDecrease:" + CdDecrease + ",}";
	}
}
