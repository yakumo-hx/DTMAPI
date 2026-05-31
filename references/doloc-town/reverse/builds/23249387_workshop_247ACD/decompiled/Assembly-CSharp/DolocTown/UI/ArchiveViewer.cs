using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ArchiveViewer : DolocUIPanel
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text subhead;

	[SerializeField]
	private Text content;

	[SerializeField]
	private CanvasGroup contentGroup;

	[SerializeField]
	public ScrollRect rect;

	[SerializeField]
	private Text emptyHint;

	public void Render(DocumentData data)
	{
		title.text = data.title;
		subhead.text = data.author;
		content.text = data.content;
		subhead.gameObject.SetActive(!data.author.IsNullOrEmpty());
	}

	public void SetEmpty(bool value)
	{
		contentGroup.alpha = ((!value) ? 1 : 0);
		emptyHint.gameObject.SetActive(value);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		emptyHint.text = base.staticTexts.CollectionPanelItemUnknown;
	}
}
