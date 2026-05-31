using DolocTown.Config.Player;
using DolocTown.GameData;

namespace DolocTown;

public class AgentEquipmentFunctionAmoeba : AgentEquipmentFunction
{
	public AgentEquipmentFunctionAmoeba(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
	}
}
