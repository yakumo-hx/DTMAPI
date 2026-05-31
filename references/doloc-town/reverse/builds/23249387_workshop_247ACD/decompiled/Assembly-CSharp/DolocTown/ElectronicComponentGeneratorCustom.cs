using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public class ElectronicComponentGeneratorCustom : ElectronicComponentGenerator
{
	[JsonProperty]
	[DebugInfo("当前缓存的电力")]
	private float power;

	public ElectronicComponentGeneratorCustom(Equipment equipment)
		: base(equipment)
	{
		power = 0f;
	}

	[JsonConstructor]
	public ElectronicComponentGeneratorCustom(float power)
	{
		this.power = power;
	}

	public void Charge(float power)
	{
		if (!(power <= 0f))
		{
			this.power += power;
		}
	}

	protected override float GeneratePower()
	{
		float result = power;
		power = 0f;
		return result;
	}
}
