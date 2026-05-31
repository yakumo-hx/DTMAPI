using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

public abstract class ElectronicComponentGenerator : ElectronicComponent, IElectronicComponentGenerator, IElectronicComponent
{
	protected bool shouldStop;

	[DebugInfo("是否正在发电", Color = "green")]
	public bool IsGenerating => !shouldStop;

	public bool ShouldStop
	{
		get
		{
			return shouldStop;
		}
		set
		{
			shouldStop = value;
		}
	}

	private float PowerIncreaseRatio
	{
		get
		{
			if (equipment.Host != null)
			{
				return equipment.CurrentRoom.RoomEffectInfo.PowerGenerationAddition + 1f;
			}
			return 1f;
		}
	}

	public float PowerGenerated => GeneratePower() * PowerIncreaseRatio;

	protected ElectronicComponentGenerator(Equipment equipment)
		: base(equipment)
	{
	}

	[JsonConstructor]
	protected ElectronicComponentGenerator()
	{
	}

	protected abstract float GeneratePower();
}
