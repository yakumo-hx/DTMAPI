using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class DroneComponentRenderer : GameEntity
{
	public SpriteRenderer spriteRenderer { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}
}
