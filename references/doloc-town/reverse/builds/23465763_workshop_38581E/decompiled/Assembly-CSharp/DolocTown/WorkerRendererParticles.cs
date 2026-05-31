using UnityEngine;

namespace DolocTown;

public abstract class WorkerRendererParticles<T> : WorkerRenderer where T : EquipmentParticleSystemRenderer
{
	public virtual Vector2 ParticlePosition => equipment.PositionTop;

	public WorkerRendererParticles(Equipment equipment)
		: base(equipment)
	{
	}

	public override void OnFailed()
	{
	}

	public override void OnIdle()
	{
	}

	public override void OnStop()
	{
		base.EquipmentRenderer.RemoveRenderComponent<T>();
	}

	public override void OnRemove()
	{
		base.EquipmentRenderer.RemoveRenderComponent<T>();
	}

	public override void OnWork()
	{
		T renderComponent = base.EquipmentRenderer.GetRenderComponent<T>();
		renderComponent.position2d = ParticlePosition;
		renderComponent.Play();
	}

	public override void OnWorkDone()
	{
	}
}
