using UnityEngine;

namespace DolocTown;

[WorkerRenderer("industrialsmoke")]
public class WorkerRendererIndustrialSmoke : WorkerRendererParticles<PotSmokeRenderer>
{
	public override Vector2 ParticlePosition => equipment.PositionCenter;

	public WorkerRendererIndustrialSmoke(Equipment equipment)
		: base(equipment)
	{
	}
}
