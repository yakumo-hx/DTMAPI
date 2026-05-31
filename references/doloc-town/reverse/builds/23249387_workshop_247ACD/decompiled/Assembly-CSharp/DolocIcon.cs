using DolocTown.UI;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DolocIcon : DolocUiRecyclableObject
{
	[SerializeField]
	public Image backgroundImg;

	[SerializeField]
	public Image iconImg;

	public virtual float alpha
	{
		get
		{
			return backgroundImg.color.a;
		}
		set
		{
			backgroundImg.SetAlpha(value);
			iconImg.SetAlpha(value);
		}
	}

	public float BackgroundAlpha
	{
		set
		{
			backgroundImg.SetAlpha(value);
		}
	}

	public Sprite backgroundSprite
	{
		get
		{
			return backgroundImg.sprite;
		}
		set
		{
			if (!(backgroundImg == null))
			{
				backgroundImg.sprite = value;
				base.size = value.rect.size * 4f;
				iconImg.rectTransform.sizeDelta = base.size;
			}
		}
	}

	public Vector2 iconSize
	{
		get
		{
			if (iconImg == null)
			{
				return Vector2.zero;
			}
			return iconImg.rectTransform.sizeDelta;
		}
		set
		{
			if (!(iconImg == null))
			{
				iconImg.rectTransform.sizeDelta = value;
			}
		}
	}

	public Color backgroundColor
	{
		get
		{
			if (backgroundImg == null)
			{
				return Color.clear;
			}
			return backgroundImg.color;
		}
		set
		{
			if (!(backgroundImg == null))
			{
				backgroundImg.color = value;
			}
		}
	}

	public Sprite iconSprite
	{
		get
		{
			if (iconImg == null)
			{
				return null;
			}
			return iconImg.sprite;
		}
		set
		{
			if (!(iconImg == null))
			{
				iconImg.sprite = value;
			}
		}
	}

	public Material iconMaterial
	{
		get
		{
			if (iconImg == null)
			{
				return null;
			}
			return iconImg.material;
		}
		set
		{
			if (!(iconImg == null))
			{
				iconImg.material = value;
			}
		}
	}

	public Color iconColor
	{
		get
		{
			if (iconImg == null)
			{
				return Color.clear;
			}
			return iconImg.color;
		}
		set
		{
			if (!(iconImg == null))
			{
				iconImg.color = value;
			}
		}
	}

	public void RaiseUiSpriteFadeUp()
	{
		if (!(iconImg == null))
		{
			iconImg.RaiseUiSpriteFadeUp();
		}
	}

	public void RaiseUiSpriteFadeDown()
	{
		if (!(iconImg == null))
		{
			iconImg.RaiseUiSpriteFadeDown();
		}
	}

	public void RaiseUiSpriteFadeUp(Sprite sprite)
	{
		if (!(iconImg == null))
		{
			iconImg.RaiseUiSpriteFadeUp(sprite);
		}
	}

	public void RaiseUiSpriteFadeDown(Sprite sprite)
	{
		if (!(iconImg == null))
		{
			iconImg.RaiseUiSpriteFadeDown(sprite);
		}
	}
}
