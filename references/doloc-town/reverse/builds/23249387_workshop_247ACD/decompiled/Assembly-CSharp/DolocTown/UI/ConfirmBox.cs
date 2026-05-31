using DolocTown.Config.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class ConfirmBox : AutoSizeUIPanel
{
	[SerializeField]
	private Text textTitle;

	[SerializeField]
	private Text textContent;

	[SerializeField]
	private Text textSignature;

	[SerializeField]
	public DolocButtonComponent buttonConfirm;

	[SerializeField]
	private float minWidth = 160f;

	[SerializeField]
	private float maxWidth = 750f;

	[SerializeField]
	private LayoutElement[] layoutElements;

	protected override void __Init()
	{
		base.__Init();
		buttonConfirm.onClick.AddListener(DolocAPI.UIRaiseConfirm);
	}

	public void SetContent(string content)
	{
		textContent.alignment = TextAnchor.MiddleCenter;
		SetText(textTitle, string.Empty);
		SetText(textContent, content);
		SetText(textSignature, string.Empty);
		AdjustWidth();
		RebuildLayout();
	}

	public void SetContent(AlignmentText title, AlignmentText content, AlignmentText signature)
	{
		SetText(textTitle, title);
		SetText(textContent, content);
		SetText(textSignature, signature);
		AdjustWidth();
		RebuildLayout();
	}

	private void AdjustWidth()
	{
		if (!layoutElements.IsNullOrEmpty())
		{
			float preferredWidth = Mathf.Clamp(textContent.preferredWidth, minWidth, maxWidth);
			LayoutElement[] array = layoutElements;
			foreach (LayoutElement obj in array)
			{
				obj.minWidth = preferredWidth;
				obj.preferredWidth = preferredWidth;
			}
		}
	}
}
