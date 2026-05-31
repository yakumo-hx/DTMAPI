namespace DolocTown;

[WorkerRenderer("lowpowertip")]
public class WorkerRendererLowPower_Tip : WorkerRenderer
{
	public WorkerRendererLowPower_Tip(Equipment equipment)
		: base(equipment)
	{
	}

	public override void OnFailed()
	{
	}

	public override void OnStop()
	{
		base.EquipmentRenderer.HideStateRenderer();
	}

	public override void OnRemove()
	{
		base.EquipmentRenderer.HideStateRenderer();
	}

	public override void OnWorkDone()
	{
	}

	public override void OnWork()
	{
	}

	public override void OnIdle()
	{
		base.EquipmentRenderer.SetStateRendererStatus(state: false, base.EquipmentRenderer.Equipment.PositionCenter);
	}
}
