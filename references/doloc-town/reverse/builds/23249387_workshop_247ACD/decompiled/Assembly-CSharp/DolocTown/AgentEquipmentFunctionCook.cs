using DolocTown.Config.Player;
using DolocTown.GameData;

namespace DolocTown;

public class AgentEquipmentFunctionCook : AgentEquipmentFunction
{
	public readonly AgentEquipmentFuncProtoCook func;

	public AgentEquipmentFunctionCook(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		func = (AgentEquipmentFuncProtoCook)skill.Function;
	}
}
