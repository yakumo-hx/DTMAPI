using DolocTown.Config.Drone;

namespace DolocTown;

public class DroneFunctionRestrictionReleaser : DroneFunction
{
	private readonly DroneFunctionProtoRestrictionReleaser _proto;

	public DroneFunctionRestrictionReleaser(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		_proto = (DroneFunctionProtoRestrictionReleaser)proto;
	}

	public override float OnCostPower(float power)
	{
		return power * (1f + _proto.PowerCostIncrease);
	}

	public override int HandleDamage(int originDamage)
	{
		return (int)((float)originDamage * (1f + _proto.DamageIncrease));
	}
}
