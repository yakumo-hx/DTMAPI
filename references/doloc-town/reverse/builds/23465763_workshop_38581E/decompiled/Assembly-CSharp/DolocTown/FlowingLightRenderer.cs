using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class FlowingLightRenderer : DolocRecyclableObject
{
	private SpriteRenderer spriteRenderer;

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		spriteRenderer.ToggleItemShine();
	}

	private void Start()
	{
		Init();
	}
}
