using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemBorder : DolocUiEntity
{
	[SerializeField]
	private Image border;

	[SerializeField]
	private Color thickBorderColor;

	[SerializeField]
	private Sprite thickBorderSprite;

	[SerializeField]
	private Color thinBorderColor;

	[SerializeField]
	private Sprite thinBorderSprite;

	[SerializeField]
	private Image arrow;

	private Transform _anchor;

	private Sequence moveTween;

	private BorderType currentType;

	private Transform anchor
	{
		get
		{
			if (_anchor == null)
			{
				_anchor = new GameObject("border_anchor").transform;
				_anchor.parent = base.transform;
			}
			return _anchor;
		}
	}

	private Color borderColor
	{
		set
		{
			border.color = value;
		}
	}

	private Sprite sprite
	{
		set
		{
			border.sprite = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		border.gameObject.SetActive(value: false);
		arrow.gameObject.SetActive(value: false);
		SetVisible(value: true);
	}

	public void Show()
	{
		border.gameObject.SetActive(value: true);
	}

	public void Hide()
	{
		border.gameObject.SetActive(value: false);
		SetBorderType(BorderType.ThinBorder);
	}

	public void HoverTo(DolocUiObject targetObj, bool useAnimation = true, BorderType borderType = BorderType.ThinBorder)
	{
		HoverTo(targetObj.rectTransform, useAnimation, borderType);
	}

	public void HoverTo(RectTransform rectTransform, bool useAnimation = true, BorderType borderType = BorderType.ThinBorder)
	{
		SetBorderType(borderType);
		anchor.SetParent(rectTransform);
		Vector2 localPositionByAnchor = rectTransform.GetLocalPositionByAnchor(UIAlignmentType.Center);
		border.rectTransform.sizeDelta = rectTransform.sizeDelta;
		moveTween?.Kill();
		if (!useAnimation || !border.gameObject.activeSelf || currentType == BorderType.Arrow)
		{
			anchor.localPosition = localPositionByAnchor;
			border.gameObject.SetActive(value: true);
		}
		else
		{
			moveTween = DOTween.Sequence();
			moveTween.Append(anchor.DOLocalMove(localPositionByAnchor, 0.2f)).SetEase(Ease.OutExpo);
		}
	}

	private void Update()
	{
		if (border.gameObject.activeSelf)
		{
			border.transform.position = anchor.position;
		}
	}

	private void SetBorderType(BorderType borderType)
	{
		if (currentType == borderType)
		{
			return;
		}
		currentType = borderType;
		switch (borderType)
		{
		case BorderType.ThinBorder:
			borderColor = thinBorderColor;
			sprite = thinBorderSprite;
			arrow.gameObject.SetActive(value: false);
			break;
		case BorderType.Arrow:
			borderColor = DolocColor.empty;
			DolocAPI.DelayFrame(delegate
			{
				arrow.gameObject.SetActive(currentType == BorderType.Arrow);
			});
			break;
		default:
			throw new ArgumentOutOfRangeException("borderType", borderType, null);
		}
	}
}
