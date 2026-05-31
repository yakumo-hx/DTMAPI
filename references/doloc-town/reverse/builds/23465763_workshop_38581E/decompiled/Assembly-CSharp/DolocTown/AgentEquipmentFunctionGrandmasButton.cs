using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionGrandmasButton : AgentEquipmentFunction
{
	public AgentEquipmentFunctionGrandmasButton(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		agentEquipmentParams.recoveryAdditionPercentOnNap += ((AgentEquipmentFuncProtoGrandmasButton)skill.Function).RecoveryIncrease;
		return agentEquipmentParams;
	}
}
