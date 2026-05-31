using UnityEngine;

namespace DolocTown;

public class Lantern : DolocObject
{
	[SerializeField]
	private Sprite maskSprite;

	private void RenderAsLantern()
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!(component == null))
		{
			component.RenderAsLantern(maskSprite);
		}
	}

	public void OnEnable()
	{
		RenderAsLantern();
	}
}
