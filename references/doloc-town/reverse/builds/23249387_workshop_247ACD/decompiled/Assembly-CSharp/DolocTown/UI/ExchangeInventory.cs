using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class ExchangeInventory : DolocUiEntity
{
	[SerializeField]
	private Image imgIcon;

	[SerializeField]
	private Image subscript;

	[SerializeField]
	private TMP_Text textCount;

	[SerializeField]
	private Vector2 offset = new Vector2(32f, 32f);

	private Transform _anchor;

	private bool shouldUpdatePosition;

	private Transform anchor
	{
		get
		{
			if (_anchor == null)
			{
				_anchor = new GameObject("buffer_anchor").transform;
				_anchor.parent = base.transform;
			}
			return _anchor;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		Render(null, updatePosition: false);
		subscript.color = DolocColor.empty;
	}

	public void HoverTo(RectTransform rectTransform)
	{
		anchor.SetParent(rectTransform);
		Vector2 localPositionByAnchor = rectTransform.GetLocalPositionByAnchor(UIAlignmentType.Center);
		anchor.localPosition = localPositionByAnchor;
	}

	public void Render(Item item)
	{
		Render(item, updatePosition: true);
	}

	public void Render(Item item, bool updatePosition)
	{
		if (item is IHasSubscript { HasSubscript: not false } hasSubscript)
		{
			subscript.sprite = hasSubscript.SubscriptSprite;
			subscript.color = Color.white;
		}
		else
		{
			subscript.color = Color.clear;
		}
		if (item == null)
		{
			SetVisible(value: false);
			imgIcon.sprite = null;
			imgIcon.color = DolocColor.empty;
			textCount.text = string.Empty;
			shouldUpdatePosition = false;
			if (updatePosition)
			{
				UpdatePosition();
			}
		}
		else
		{
			SetVisible(value: true);
			imgIcon.sprite = item.uiSprite;
			imgIcon.color = Color.white;
			textCount.text = (item.noOverlay ? string.Empty : item.count.ToString());
			shouldUpdatePosition = true;
			if (updatePosition)
			{
				UpdatePosition();
			}
		}
	}

	private void Update()
	{
		if (shouldUpdatePosition)
		{
			UpdatePosition();
		}
	}

	private void UpdatePosition()
	{
		if (DolocAPI.cursorManager.HiddenCursorByGamepad)
		{
			base.transform.position = anchor.position + (Vector3)offset;
		}
		else
		{
			base.transform.position = DolocAPI.UserInput.MousePositionGetter() + offset;
		}
	}
}
