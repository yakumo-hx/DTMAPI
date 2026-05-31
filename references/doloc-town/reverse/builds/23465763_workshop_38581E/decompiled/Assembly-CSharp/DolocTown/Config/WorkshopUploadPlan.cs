namespace DolocTown.Config;

public class WorkshopUploadPlan
{
	public WorkshopUploadMode mode { get; }

	public ulong workshopId { get; }

	public WorkshopUploadPlan(WorkshopUploadMode mode, ulong workshopId)
	{
		this.mode = mode;
		this.workshopId = workshopId;
	}
}
