namespace DolocTown;

[WorkerRenderer("rotatedarrow")]
public class WorkerRendererRotatedArrow : WorkerRenderer
{
	public WorkerRendererRotatedArrow(Equipment equipment)
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
		base.EquipmentRenderer.HideStateRenderer();
	}

	public override void OnRemove()
	{
		base.EquipmentRenderer.RemoveStateRenderer();
	}

	public override void OnWork()
	{
		base.EquipmentRenderer.SetStateRendererStatus(state: true, base.EquipmentRenderer.Equipment.PositionCenter);
	}

	public override void OnWorkDone()
	{
	}
}
