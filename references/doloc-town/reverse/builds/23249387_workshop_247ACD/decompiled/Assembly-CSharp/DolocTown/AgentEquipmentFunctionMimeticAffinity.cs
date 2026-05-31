using DolocTown.Config.Player;
using DolocTown.GameData;

namespace DolocTown;

public class AgentEquipmentFunctionMimeticAffinity : AgentEquipmentFunction
{
	public override bool IsChomperMimicryForbidden => true;

	public AgentEquipmentFunctionMimeticAffinity(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
	}
}
