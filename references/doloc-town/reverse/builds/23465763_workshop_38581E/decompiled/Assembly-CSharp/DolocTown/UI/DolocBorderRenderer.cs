using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocBorderRenderer : DolocUiRecyclableObject
{
	[SerializeField]
	private Image img;

	public Color Color
	{
		get
		{
			return img.color;
		}
		set
		{
			img.color = value;
		}
	}

	public float Alpha
	{
		get
		{
			return img.color.a;
		}
		set
		{
			DolocUtils.setAlpha(img, value);
		}
	}

	public Sprite Sprite
	{
		get
		{
			return img.sprite;
		}
		set
		{
			img.sprite = value;
			base.size = img.sprite.rect.size * 4f;
		}
	}

	public Vector2 SizeDelta
	{
		get
		{
			return img.rectTransform.sizeDelta;
		}
		set
		{
			img.rectTransform.sizeDelta = value;
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		Color = DolocColor.white;
	}

	public void ShineArea()
	{
		if (!(Alpha > 0f))
		{
			Alpha = 1f;
			img.DOFade(0f, 2f);
		}
	}
}
