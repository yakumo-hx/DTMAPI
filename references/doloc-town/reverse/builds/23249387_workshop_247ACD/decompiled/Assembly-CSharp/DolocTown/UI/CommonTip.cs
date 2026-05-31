using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class CommonTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Image image;

	public Sprite sprite
	{
		get
		{
			if (image == null)
			{
				return null;
			}
			return image.sprite;
		}
		set
		{
			if (image != null)
			{
				image.sprite = value;
				if (value != null)
				{
					base.size = value.rect.size * 4f;
				}
			}
		}
	}

	public Material material
	{
		get
		{
			if (image == null)
			{
				return null;
			}
			return image.material;
		}
		set
		{
			if (image != null)
			{
				image.material = value;
			}
		}
	}
}
