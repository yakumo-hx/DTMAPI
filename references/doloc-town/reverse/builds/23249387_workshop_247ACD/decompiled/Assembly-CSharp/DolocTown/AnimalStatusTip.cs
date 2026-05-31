using System;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown;

[RequireComponent(typeof(Image))]
public class AnimalStatusTip : DolocUiRecyclableObject
{
	private Image backgroundImage;

	[SerializeField]
	private Image image01;

	[SerializeField]
	private Image image02;

	[SerializeField]
	private Vector2 debugOffset;

	public Func<Vector2> PositionGetter { get; set; }

	private int imageCount
	{
		set
		{
			RectTransform component = backgroundImage.GetComponent<RectTransform>();
			switch (value)
			{
			case 1:
				component.sizeDelta = new Vector2(60f, 68f);
				break;
			case 2:
				component.sizeDelta = new Vector2(108f, 68f);
				break;
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		backgroundImage = GetComponent<Image>();
	}

	public void Render(Sprite sprite01, Sprite sprite02)
	{
		if (sprite01 == null && sprite02 == null)
		{
			SetVisible(value: false);
			return;
		}
		SetVisible(value: true);
		if (sprite01 != null && sprite02 != null)
		{
			imageCount = 2;
			image01.sprite = sprite01;
			image02.sprite = sprite02;
			image01.enabled = true;
			image02.enabled = true;
		}
		else
		{
			Sprite sprite3 = sprite01 ?? sprite02;
			imageCount = 1;
			image01.sprite = sprite3;
			image01.enabled = true;
			image02.enabled = false;
		}
	}

	private void Update()
	{
		if (PositionGetter != null)
		{
			Vector2 vector = PositionGetter() + debugOffset;
			base.transform.position = DolocAPI.WorldToScreen(vector);
		}
	}
}
