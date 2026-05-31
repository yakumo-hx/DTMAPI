using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DocumentViewer : DolocUiObject, IScrollContentRect
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtAuthor;

	[SerializeField]
	private Text txtContent;

	[SerializeField]
	private Text txtEmptyHint;

	[SerializeField]
	private CanvasGroup contentCanvas;

	[SerializeField]
	private ScrollRect _scrollRect;

	public bool isEmpty { get; private set; }

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		SetEmpty(value: true);
	}

	public void Render(string title, string author, string content)
	{
		if (string.IsNullOrEmpty(title))
		{
			SetEmpty(value: true);
			return;
		}
		SetEmpty(value: false);
		txtTitle.text = title;
		txtAuthor.text = author;
		txtContent.text = content;
		RebuildLayout();
		scrollRect.verticalScrollbar.value = 1f;
	}

	private void SetEmpty(bool value)
	{
		isEmpty = value;
		contentCanvas.alpha = ((!value) ? 1 : 0);
		txtEmptyHint.gameObject.SetActive(value);
	}

	public void Hide()
	{
		SetEmpty(value: true);
	}

	public void SetEmptyInfo(string info)
	{
		txtEmptyHint.text = info;
	}

	public void OnMove()
	{
	}
}
