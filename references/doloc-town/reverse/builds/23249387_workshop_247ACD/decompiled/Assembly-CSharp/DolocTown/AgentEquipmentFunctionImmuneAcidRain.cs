using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionImmuneAcidRain : AgentEquipmentFunction
{
	public AgentEquipmentFunctionImmuneAcidRain(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		agentEquipmentParams.immuneAcidRain = true;
		return agentEquipmentParams;
	}
}
