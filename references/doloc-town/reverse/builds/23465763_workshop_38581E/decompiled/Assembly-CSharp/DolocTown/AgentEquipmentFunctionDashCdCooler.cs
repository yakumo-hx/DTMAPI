using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionDashCdCooler : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoDashCdCooler _func;

	public AgentEquipmentFunctionDashCdCooler(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoDashCdCooler)skill.Function;
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		agentEquipmentParams.dashCdDecrease += _func.CdDecrease;
		return agentEquipmentParams;
	}
}
