namespace DolocTown.GameData;

public readonly struct ResourceGenInfoProto
{
	public static readonly ResourceGenInfoProto Empty = new ResourceGenInfoProto(TerrainConstraintProto.Empty, shouldGen: false);

	public readonly TerrainConstraintProto constraint;

	public readonly bool shouldGen;

	public ResourceGenInfoProto(TerrainConstraintProto constraintProto, bool shouldGen)
	{
		constraint = constraintProto;
		this.shouldGen = shouldGen;
	}
}
