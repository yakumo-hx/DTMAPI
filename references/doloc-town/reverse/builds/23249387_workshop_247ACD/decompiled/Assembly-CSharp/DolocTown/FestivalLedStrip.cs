using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class FestivalLedStrip : DolocObject
{
	[SerializeField]
	private Sprite maskSprite;

	public void RenderAsFestivalLedStrip()
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!(component.sprite == null) && !(maskSprite == null))
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetTexture("_MainTex", component.sprite.texture);
			materialPropertyBlock.SetTexture("_FlickerMask", maskSprite.texture);
			materialPropertyBlock.SetVector("_MainUVInfos", DataUtility.GetOuterUV(component.sprite));
			materialPropertyBlock.SetVector("_MaskUVInfos", DataUtility.GetOuterUV(maskSprite));
			component.SetPropertyBlock(materialPropertyBlock);
		}
	}
}
