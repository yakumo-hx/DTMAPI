namespace DolocTown;

public class ApplianceHandle
{
	private readonly IApplianceHost host;

	public ApplianceHandle(IApplianceHost host)
	{
		this.host = host;
	}

	public void UpdatePerSec()
	{
		if (host.isWorking)
		{
			host.OnApplianceWork();
			if (host.workCounter.Tick())
			{
				host.isWorking = false;
			}
		}
		else if (!host.shouldWork)
		{
			host.OnApplianceTurnOff();
		}
		else if (host.Appliance.Launch())
		{
			host.isWorking = true;
			host.workCounter.Reset();
			host.OnApplianceStart();
		}
		else
		{
			host.OnApplianceIdle();
		}
	}
}
