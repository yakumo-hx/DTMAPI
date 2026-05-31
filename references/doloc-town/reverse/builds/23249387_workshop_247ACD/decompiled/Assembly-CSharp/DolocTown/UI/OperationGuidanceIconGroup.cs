using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class OperationGuidanceIconGroup : DolocUiObject
{
	[SerializeField]
	private Image[] icons;

	public int IconCount => icons.Length;

	protected override void __Init()
	{
		base.__Init();
		if (icons == null)
		{
			icons = Array.Empty<Image>();
		}
	}

	public void SetIcons(Sprite[] sprites)
	{
		Sprite[] array = sprites.Where((Sprite x) => x != null).ToArray();
		for (int i = 0; i < array.Length && i < icons.Length; i++)
		{
			SetSprite(icons[i], array[i], autoSize: true);
			icons[i].color = Color.white;
		}
		for (int j = sprites.Length; j < icons.Length; j++)
		{
			icons[j].color = DolocColor.empty;
		}
	}
}
