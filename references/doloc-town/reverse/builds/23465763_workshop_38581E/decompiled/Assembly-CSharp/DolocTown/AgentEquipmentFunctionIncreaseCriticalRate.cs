using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionIncreaseCriticalRate : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoIncreaseCriticalRate _func;

	public AgentEquipmentFunctionIncreaseCriticalRate(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoIncreaseCriticalRate)skill.Function;
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		agentEquipmentParams.criticalRateChanged += _func.CriticalRate;
		return agentEquipmentParams;
	}
}
