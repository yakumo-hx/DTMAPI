using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverSODrop : BulletMoverSO
{
	[SerializeField]
	public float gravity;

	public override BulletMoverProto CreateProto(string name)
	{
		return new BulletMoverProtoDrop(name, gravity);
	}
}
