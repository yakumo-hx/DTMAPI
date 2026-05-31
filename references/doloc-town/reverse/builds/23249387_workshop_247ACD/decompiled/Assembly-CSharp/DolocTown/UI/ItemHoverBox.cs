using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemHoverBox : HoverBoxBase
{
	[SerializeField]
	private Text txtName;

	[SerializeField]
	private Text txtDesc;

	[SerializeField]
	private Text txtInfo1;

	[SerializeField]
	private Text txtInfo2;

	[SerializeField]
	private Text txtPrice;

	[SerializeField]
	private Text textType;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private EffectGroupViewer effectGroupViewer;

	[SerializeField]
	private LayoutElement nameLayoutElement;

	[SerializeField]
	private LayoutElement typeLayoutElement;

	[SerializeField]
	private float namePreferredWidth = 248f;

	[SerializeField]
	private float typePreferredWidth = 168f;

	[SerializeField]
	private float maxWidth = 416f;

	private List<Sprite> _levelSprites;

	public bool isHide => canvasGroup.alpha == 0f;

	protected override void __Init()
	{
		base.__Init();
		_levelSprites = new List<Sprite>
		{
			LocSprites.UI_INFOICON_STAR,
			LocSprites.UI_INFOICON_STAR2,
			LocSprites.UI_INFOICON_STAR3
		};
		effectGroupViewer.Init();
	}

	public void RenderAndShow(ItemData data)
	{
		if (!data.notEmpty)
		{
			canvasGroup.alpha = 0f;
			return;
		}
		canvasGroup.alpha = 1f;
		SetText(txtName, data.title);
		SetText(txtDesc, data.description);
		SetText(textType, data.type);
		SetText(txtInfo1, data.info1);
		SetText(txtInfo2, data.info2);
		SetText(txtPrice, data.price);
		effectGroupViewer.Render(data.effectGroup);
		Show();
		float preferredWidth = txtName.preferredWidth;
		float preferredWidth2 = textType.preferredWidth;
		if (preferredWidth + preferredWidth2 <= maxWidth)
		{
			nameLayoutElement.preferredWidth = maxWidth - preferredWidth2;
			typeLayoutElement.preferredWidth = preferredWidth2;
		}
		else
		{
			nameLayoutElement.preferredWidth = maxWidth - Mathf.Min(preferredWidth2, typePreferredWidth);
			typeLayoutElement.preferredWidth = maxWidth - Mathf.Min(preferredWidth, namePreferredWidth);
		}
		RebuildLayout();
	}

	private void Show()
	{
		canvasGroup.alpha = 1f;
	}

	public override void Hide()
	{
		canvasGroup.alpha = 0f;
	}
}
