using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionFellCountAdditionOre : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoFellCountAdditionOre _func;

	public AgentEquipmentFunctionFellCountAdditionOre(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoFellCountAdditionOre)skill.Function;
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		agentEquipmentParams.fellCoundAdditionOre += _func.FellCountIncrease;
		return agentEquipmentParams;
	}
}
