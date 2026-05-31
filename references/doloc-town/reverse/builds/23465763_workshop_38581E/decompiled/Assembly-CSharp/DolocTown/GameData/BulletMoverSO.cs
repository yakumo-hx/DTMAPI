using System;

namespace DolocTown.GameData;

[Serializable]
public abstract class BulletMoverSO
{
	public abstract BulletMoverProto CreateProto(string name);
}
