using DolocTown.Config.Drone;

namespace DolocTown;

public class DroneFunctionCapacitance : DroneFunction
{
	private bool isCharging;

	private readonly DroneFunctionProtoCapacitance protoCapacitance;

	public DroneFunctionCapacitance(Drone drone, DroneFunctionProto proto)
		: base(drone, proto)
	{
		protoCapacitance = (DroneFunctionProtoCapacitance)proto;
	}

	public override bool OnReceiveMessage(DroneEventType type, GameEventArgs args)
	{
		switch (type)
		{
		case DroneEventType.ReloadComplete:
			isCharging = true;
			break;
		case DroneEventType.HitEnemy:
			if (isCharging)
			{
				isCharging = false;
				AttackHitInfo value = ((GameEventArgs<AttackHitInfo>)args).value;
				if (value.IsHitEnemy)
				{
					DolocAPI.RaiseInstantAnimEffects(value.position, InstAnimEffectType.THUNDER, LocMaterials.GAME_MAT_THUNDER);
					DolocAPI.RaiseInstantAnimEffects(value.position, InstAnimEffectType.ELECTRIC_CURRENT, LocMaterials.GAME_MAT_THUNDER);
					DolocAPI.RaiseInstantPSEffects(value.position, InstantParticleEffectsType.ELECTRIC_SPARKS);
					DolocAPI.cameraController.ShakeScreen();
					int num = protoCapacitance.ExtraDamage + (int)((float)drone.weapon.weaponParams.Attack * protoCapacitance.ExtraDamagePercent);
					value.attackableObj.OnAttacked(num, ctr: false, value.position);
				}
			}
			break;
		}
		return false;
	}
}
