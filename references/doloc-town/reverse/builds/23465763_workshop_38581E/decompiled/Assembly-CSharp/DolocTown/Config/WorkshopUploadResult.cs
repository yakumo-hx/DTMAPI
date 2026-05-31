namespace DolocTown.Config;

public class WorkshopUploadResult
{
	public WorkshopUploadMode mode { get; }

	public bool success { get; }

	public ulong workshopId { get; }

	public WorkshopUploadResult(WorkshopUploadMode mode, bool success, ulong workshopId)
	{
		this.mode = mode;
		this.success = success;
		this.workshopId = workshopId;
	}
}
