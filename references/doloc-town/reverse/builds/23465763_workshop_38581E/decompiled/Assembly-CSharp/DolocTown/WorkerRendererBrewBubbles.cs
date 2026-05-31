using UnityEngine;

namespace DolocTown;

[WorkerRenderer("brewbubbles")]
public class WorkerRendererBrewBubbles : WorkerRendererParticles<BrewBubbles>
{
	public override Vector2 ParticlePosition => equipment.PositionCenter;

	public WorkerRendererBrewBubbles(Equipment equipment)
		: base(equipment)
	{
	}

	public override void OnWorkDone()
	{
		DolocAPI.RaiseInstantPSEffects(base.EquipmentRenderer.Equipment.PositionTop, InstantParticleEffectsType.BRUST_STARS);
	}
}
