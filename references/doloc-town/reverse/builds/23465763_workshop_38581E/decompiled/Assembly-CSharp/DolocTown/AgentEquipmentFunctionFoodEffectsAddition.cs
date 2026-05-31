using System.Linq;
using DolocTown.Config.Player;
using DolocTown.GameData;

namespace DolocTown;

public class AgentEquipmentFunctionFoodEffectsAddition : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoFoodEffectsAddition _func;

	public AgentEquipmentFunctionFoodEffectsAddition(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoFoodEffectsAddition)skill.Function;
	}

	public override void OnReceiveMessage(GameMessage message)
	{
		if (message.Type == GameEventType.USE_ITEM)
		{
			GameEventArgs args2 = message.Args;
			GameEventArgsString args = args2 as GameEventArgsString;
			if (args != null && !_func.ItemNames.All((string x) => x != args.value) && DolocAPI.GenerateItem(args.value) is IEatable eatable)
			{
				eatable.DoEffects(_func.AdditionRate);
			}
		}
	}
}
