using System.Linq;
using DolocTown.Config.Player;
using DolocTown.GameData;

namespace DolocTown;

public class AgentEquipmentFunctionShepherd : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoShepherd _func;

	public AgentEquipmentFunctionShepherd(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoShepherd)proto;
	}

	public override bool IsShepherdActive(string name, out int moodIncrease)
	{
		moodIncrease = 0;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		if (!_func.AnimalNames.Contains(name))
		{
			return false;
		}
		moodIncrease = _func.MoodIncrease;
		return true;
	}
}
