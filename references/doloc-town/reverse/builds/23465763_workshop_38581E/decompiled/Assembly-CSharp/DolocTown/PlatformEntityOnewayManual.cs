using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(EdgeCollider2D), typeof(PlatformEffector2D))]
public class PlatformEntityOnewayManual : PlatformEntityOneway
{
	private void Start()
	{
		Init();
	}
}
