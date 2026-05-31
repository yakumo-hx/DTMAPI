using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public abstract class AgentEquipmentFuncProto : BeanBase
{
	public AgentEquipmentFuncProto(JSONNode _json)
	{
	}

	public AgentEquipmentFuncProto()
	{
	}

	public static AgentEquipmentFuncProto DeserializeAgentEquipmentFuncProto(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"AgentEquipmentFuncProtoExtraResource" => new AgentEquipmentFuncProtoExtraResource(_json), 
			"AgentEquipmentFuncProtoImmuneAcidRain" => new AgentEquipmentFuncProtoImmuneAcidRain(_json), 
			"AgentEquipmentFuncProtoAreaSpeedUp" => new AgentEquipmentFuncProtoAreaSpeedUp(_json), 
			"AgentEquipmentFuncProtoDashCdCooler" => new AgentEquipmentFuncProtoDashCdCooler(_json), 
			"AgentEquipmentFuncProtoHerbPackage" => new AgentEquipmentFuncProtoHerbPackage(_json), 
			"AgentEquipmentFuncProtoGrandmasButton" => new AgentEquipmentFuncProtoGrandmasButton(_json), 
			"AgentEquipmentFuncProtoFellCountAdditionOre" => new AgentEquipmentFuncProtoFellCountAdditionOre(_json), 
			"AgentEquipmentFuncProtoFoodEffectsAddition" => new AgentEquipmentFuncProtoFoodEffectsAddition(_json), 
			"AgentEquipmentFuncProtoDisguise" => new AgentEquipmentFuncProtoDisguise(_json), 
			"AgentEquipmentFuncProtoShepherd" => new AgentEquipmentFuncProtoShepherd(_json), 
			"AgentEquipmentFuncProtoFishTank" => new AgentEquipmentFuncProtoFishTank(_json), 
			"AgentEquipmentFuncProtoShield" => new AgentEquipmentFuncProtoShield(_json), 
			"AgentEquipmentFuncProtoLightningConductor" => new AgentEquipmentFuncProtoLightningConductor(_json), 
			"AgentEquipmentFuncProtoAmoeba" => new AgentEquipmentFuncProtoAmoeba(_json), 
			"AgentEquipmentFuncProtoIncreaseCriticalRate" => new AgentEquipmentFuncProtoIncreaseCriticalRate(_json), 
			"AgentEquipmentFuncProtoMimeticAffinity" => new AgentEquipmentFuncProtoMimeticAffinity(_json), 
			"AgentEquipmentFuncProtoCook" => new AgentEquipmentFuncProtoCook(_json), 
			"AgentEquipmentFuncProtoCounterBack" => new AgentEquipmentFuncProtoCounterBack(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
