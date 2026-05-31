using UnityEngine;

namespace DolocTown;

public struct MotionParamModifier
{
	public Vector2 jumpForceModifier;

	public Vector2 moveSpeedModifier;

	public int envModerateCount;

	public readonly int EnvModerateEnabled
	{
		get
		{
			if (envModerateCount <= 0)
			{
				return 0;
			}
			return 1;
		}
	}
}
