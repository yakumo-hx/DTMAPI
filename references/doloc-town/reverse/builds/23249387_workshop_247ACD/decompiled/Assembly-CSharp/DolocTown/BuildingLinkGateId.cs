namespace DolocTown;

public struct BuildingLinkGateId
{
	public BuildingLinkType type;

	public int index;

	public BuildingLinkGateId(BuildingLinkType type, int index)
	{
		this.type = type;
		this.index = index;
	}

	public override string ToString()
	{
		return $"{type}-{index}";
	}
}
