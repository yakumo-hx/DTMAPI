using DolocTown.Config.Drone;

namespace DolocTown;

public class DroneFunctionCrow : DroneFunction
{
	private readonly DroneFunctionProtoCrow protoCrow;

	public DroneFunctionCrow(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoCrow = (DroneFunctionProtoCrow)proto;
	}

	public override bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		if (type != DroneEventType.EnemyDead)
		{
			return false;
		}
		drone.Charge(protoCrow.PowerRecv * drone.structure.droneParams.PowerCapacity);
		return false;
	}
}
