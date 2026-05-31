using DolocTown.Config.Player;
using DolocTown.GameData;
using RedSaw;

namespace DolocTown;

public class AgentEquipmentFunctionCounterBack : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoCounterBack _func;

	public AgentEquipmentFunctionCounterBack(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoCounterBack)skill.Function;
	}

	public override void OnReceiveMessage(GameMessage message)
	{
		if (message.Type == GameEventType.HURT_BY_MONSTER && RandomUtils.Dice(_func.Probability))
		{
			DolocAPI.AgentController.skillManager.UseSkillFromItemName(_func.SkillId, AgentSkillManager.TriggerType.NONE);
		}
	}
}
