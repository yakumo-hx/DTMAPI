using DolocTown.Config.Drone;

namespace DolocTown;

public class DroneFunctionDebugger : DroneFunction
{
	public DroneFunctionDebugger(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
	}

	public override void HandleBullet(Bullet bullet)
	{
	}
}
