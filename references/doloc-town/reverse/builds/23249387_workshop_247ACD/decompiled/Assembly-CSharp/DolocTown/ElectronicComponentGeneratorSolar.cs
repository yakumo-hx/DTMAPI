using DolocTown.Config.Equipment;
using DolocTown.Config.Time;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class ElectronicComponentGeneratorSolar : ElectronicComponentGenerator
{
	private EComProtoGeneratorSolar proto;

	[DebugInfo("发电效率", Color = "green")]
	public float Efficiency => proto.Efficiency;

	public ElectronicComponentGeneratorSolar(Equipment equipment)
		: base(equipment)
	{
		shouldStop = false;
		proto = (EComProtoGeneratorSolar)equipment.proto.ElectronicComponent;
	}

	[JsonConstructor]
	public ElectronicComponentGeneratorSolar()
	{
	}

	public override void SetEquipment(Equipment equipment)
	{
		base.SetEquipment(equipment);
		proto = (EComProtoGeneratorSolar)equipment.proto.ElectronicComponent;
	}

	protected override float GeneratePower()
	{
		if (shouldStop)
		{
			return 0f;
		}
		float num = ((DolocAPI.archiveHandle.CurrentDayPeriodType == DayPeriodType.Daytime) ? 1 : 0);
		return proto.Efficiency * equipment.Host.CurrentWeatherInfo.Sun * num;
	}
}
