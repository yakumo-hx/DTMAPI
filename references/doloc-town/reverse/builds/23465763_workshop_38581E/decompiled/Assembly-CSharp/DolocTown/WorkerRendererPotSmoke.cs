namespace DolocTown;

[WorkerRenderer("potsmoke")]
public class WorkerRendererPotSmoke : WorkerRendererParticles<PotSmokeRenderer>
{
	public WorkerRendererPotSmoke(Equipment equipment)
		: base(equipment)
	{
	}

	public override void OnWorkDone()
	{
		DolocAPI.RaiseInstantPSEffects(base.EquipmentRenderer.Equipment.PositionTop, InstantParticleEffectsType.BRUST_STARS);
	}
}
