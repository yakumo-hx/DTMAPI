using RedSaw;

namespace DolocTown;

public interface IApplianceHost
{
	ElectronicComponentAppliance Appliance { get; }

	Counter workCounter { get; }

	bool isWorking { get; set; }

	bool shouldWork { get; }

	void OnApplianceWork();

	void OnApplianceStart();

	void OnApplianceIdle();

	void OnApplianceTurnOff();
}
