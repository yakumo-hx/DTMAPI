namespace DolocTown;

public class BusStation : Station
{
	protected override void DoTransport(string markPointId)
	{
		DolocAPI.DoTransport(markPointId);
	}
}
