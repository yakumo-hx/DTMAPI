using DolocTown.Config.Equipment;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class ElectronicComponentGeneratorWind : ElectronicComponentGenerator
{
	private EComProtoGeneratorWind proto;

	[DebugInfo("发电效率", Color = "green")]
	public float Efficiency => proto.Efficiency;

	public ElectronicComponentGeneratorWind(Equipment equipment)
		: base(equipment)
	{
		shouldStop = false;
		proto = (EComProtoGeneratorWind)equipment.proto.ElectronicComponent;
	}

	[JsonConstructor]
	public ElectronicComponentGeneratorWind()
	{
	}

	public override void SetEquipment(Equipment equipment)
	{
		base.SetEquipment(equipment);
		proto = (EComProtoGeneratorWind)equipment.proto.ElectronicComponent;
	}

	protected override float GeneratePower()
	{
		if (shouldStop)
		{
			return 0f;
		}
		return proto.Efficiency * equipment.Host.CurrentWeatherInfo.Wind;
	}
}
