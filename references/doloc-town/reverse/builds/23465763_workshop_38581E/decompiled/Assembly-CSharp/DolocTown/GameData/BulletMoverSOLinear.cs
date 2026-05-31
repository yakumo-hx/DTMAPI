namespace DolocTown.GameData;

public class BulletMoverSOLinear : BulletMoverSO
{
	public override BulletMoverProto CreateProto(string name)
	{
		return new BulletMoverProtoLinear(name);
	}
}
