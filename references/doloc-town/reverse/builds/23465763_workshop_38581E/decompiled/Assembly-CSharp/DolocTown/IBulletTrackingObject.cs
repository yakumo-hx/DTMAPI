using UnityEngine;

namespace DolocTown;

public interface IBulletTrackingObject
{
	Transform transform { get; }

	bool isValid { get; }
}
