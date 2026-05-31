using UnityEngine;

namespace DolocTown.GameData;

public class BulletMoverSOTracking : BulletMoverSO
{
	[SerializeField]
	[Range(0f, 5f)]
	private float turningSpeed;

	public override BulletMoverProto CreateProto(string name)
	{
		return new BulletMoverProtoTracking(name, turningSpeed);
	}
}
