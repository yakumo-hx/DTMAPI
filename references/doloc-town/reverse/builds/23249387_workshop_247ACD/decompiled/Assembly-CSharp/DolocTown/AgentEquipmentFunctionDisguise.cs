using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionDisguise : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoDisguise _func;

	public AgentEquipmentFunctionDisguise(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoDisguise)skill.Function;
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		string[] monsterNames = _func.MonsterNames;
		foreach (string text in monsterNames)
		{
			agentEquipmentParams.ShieldSightOfMonsterTypes.Add(text);
		}
		return agentEquipmentParams;
	}
}
