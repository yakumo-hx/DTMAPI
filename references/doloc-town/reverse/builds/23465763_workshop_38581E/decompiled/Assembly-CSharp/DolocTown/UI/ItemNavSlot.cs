using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemNavSlot : DolocNavigationButton
{
	public enum GrayMode
	{
		Content,
		All
	}

	[SerializeField]
	public Color normalBgColor;

	[SerializeField]
	public Color highLightBgColor;

	[SerializeField]
	public Text labelText;

	[SerializeField]
	private Image subscriptIcon;

	[SerializeField]
	private TMP_Text countText;

	[SerializeField]
	private Image barImg;

	[SerializeField]
	private Image lockImg;

	[SerializeField]
	private CanvasGroup contentCanvas;

	public GrayMode grayMode;

	protected override void __Init()
	{
		base.__Init();
		Render();
	}

	public void Render(Item item = null, bool isSlotLocked = false)
	{
		Clear();
		if (item != null)
		{
			SetSlotLock(isSlotLocked);
			RenderItem(item.uiSprite, (!item.noOverlay) ? item.count : 0);
			if (item is IDurability durability)
			{
				SetItemDurability(durability.remainingPercent);
			}
			if (item is IHasSubscript hasSubscript)
			{
				SetItemSubscript(hasSubscript.SubscriptSprite);
			}
		}
	}

	public void Clear()
	{
		SetSlotLock();
		RenderItem();
		SetItemSubscript();
		SetItemDurability();
	}

	public void RenderItem(Sprite sprite = null, int count = 0)
	{
		if (sprite == null)
		{
			base.iconColor = DolocColor.empty;
			countText.color = DolocColor.empty;
			return;
		}
		base.iconColor = Color.white;
		countText.color = Color.white;
		base.iconSprite = sprite;
		countText.text = ((count > 0) ? count.ToString() : "");
	}

	public void RenderItem(Sprite sprite, string count)
	{
		if (sprite == null)
		{
			Render();
			return;
		}
		base.iconColor = Color.white;
		countText.color = Color.white;
		base.iconSprite = sprite;
		countText.text = count;
	}

	public void SetItemSubscript(Sprite sprite = null)
	{
		if (sprite == null)
		{
			subscriptIcon.color = DolocColor.empty;
			return;
		}
		subscriptIcon.sprite = sprite;
		subscriptIcon.color = Color.white;
	}

	public void SetItemDurability(float percent = -1f)
	{
		if (percent < 0f)
		{
			barImg.transform.localScale = new Vector3(0f, 1f, 1f);
			return;
		}
		percent = Mathf.Clamp(percent, 0.05f, 1f);
		barImg.transform.localScale = new Vector3(percent, 1f, 1f);
		Color color = ((percent < 0.35f) ? DolocColor.red : ((!(percent < 0.7f)) ? DolocColor.eyecatchUiColor_Cyan : DolocColor.orange));
		barImg.color = color;
	}

	public void SetItemProgressBar(float percent = -1f)
	{
		if (percent < 0f)
		{
			barImg.transform.localScale = new Vector3(0f, 1f, 1f);
			return;
		}
		percent = Mathf.Clamp(percent, 0.05f, 1f);
		barImg.transform.localScale = new Vector3(percent, 1f, 1f);
		barImg.color = DolocColor.green;
	}

	public void SetSlotLock(bool isSlotLocked = false)
	{
		lockImg.color = (isSlotLocked ? DolocColor.white : DolocColor.empty);
	}

	protected override void OnGrayed(bool value)
	{
		switch (grayMode)
		{
		case GrayMode.Content:
			contentCanvas.DOFade(value ? 0.3f : 1f, 0.1f);
			break;
		case GrayMode.All:
			buttonCanvasGroup.DOFade(value ? 0.3f : 1f, 0.1f);
			break;
		}
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highLightBgColor : normalBgColor);
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		DolocAPI.uiSystem.inventoryMouse.HoverTo(base.rectTransform);
	}
}
