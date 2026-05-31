using System.Linq;
using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.Params;

namespace DolocTown;

public class AgentEquipmentFunctionAreaSpeedUp : AgentEquipmentFunction
{
	private readonly AgentEquipmentFuncProtoAreaSpeedUp _func;

	public AgentEquipmentFunctionAreaSpeedUp(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
		_func = (AgentEquipmentFuncProtoAreaSpeedUp)proto;
	}

	public override AgentEquipmentParams DoExtraConfig(AgentEquipmentParams agentEquipmentParams)
	{
		if (DolocAPI.CurrentRoom == null)
		{
			return agentEquipmentParams;
		}
		string currentRoomName = DolocAPI.CurrentRoom.SceneRawName;
		if (_func.RoomNames.Any((string x) => x == currentRoomName))
		{
			agentEquipmentParams.moveSpeedAdditionPercent += _func.SpeedUp;
		}
		return agentEquipmentParams;
	}

	public override void AfterEnterRoom(Room room)
	{
		_manager.ReloadParams();
	}
}
