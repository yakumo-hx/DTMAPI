using DolocTown.Config.TechTree;

namespace DolocTown.GameData;

public struct TechPointAdder
{
	public readonly TechPointType type;

	public byte count;

	public TechPointAdder(TechPointType type, byte count)
	{
		this.type = type;
		this.count = count;
	}
}
