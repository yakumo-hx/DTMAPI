using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class EmissionObject : DolocObject
{
	[SerializeField]
	[ColorUsage(true, true)]
	private Color emissionColor = Color.white;

	[SerializeField]
	private Sprite emissionMask;

	private void Start()
	{
		Render();
	}

	private void Render()
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!(component == null))
		{
			component.Toggle2DEmission(component.sprite, emissionMask, emissionColor);
		}
	}
}
