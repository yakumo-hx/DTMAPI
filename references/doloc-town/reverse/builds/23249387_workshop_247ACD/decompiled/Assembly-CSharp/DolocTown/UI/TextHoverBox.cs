using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TextHoverBox : HoverBoxBase
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtInfo;

	[SerializeField]
	private Text txtContent;

	[SerializeField]
	private float duration = 2f;

	[SerializeField]
	private Image background;

	private Sequence _sequence;

	private void Show()
	{
		base.gameObject.SetActive(value: true);
	}

	public override void Hide()
	{
		base.gameObject.SetActive(value: false);
	}

	public void RenderAndShow(TextGroup data)
	{
		SetText(txtTitle, data.title);
		SetText(txtInfo, data.info);
		SetText(txtContent, data.content);
		Show();
		RebuildLayout();
	}
}
