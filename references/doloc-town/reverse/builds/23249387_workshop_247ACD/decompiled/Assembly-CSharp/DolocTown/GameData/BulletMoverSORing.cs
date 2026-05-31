using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverSORing : BulletMoverSO
{
	[SerializeField]
	private float angleOffset;

	public override BulletMoverProto CreateProto(string name)
	{
		return new BulletMoverProtoRing(name, angleOffset);
	}
}
