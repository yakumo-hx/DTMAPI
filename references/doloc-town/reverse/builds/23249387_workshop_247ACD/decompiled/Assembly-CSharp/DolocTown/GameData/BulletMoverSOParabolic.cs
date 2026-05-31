using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverSOParabolic : BulletMoverSO
{
	[SerializeField]
	public float Gravity;

	[SerializeField]
	public float speedY;

	public override BulletMoverProto CreateProto(string name)
	{
		return new BulletMoverProtoParabolic(name, Gravity, speedY);
	}
}
