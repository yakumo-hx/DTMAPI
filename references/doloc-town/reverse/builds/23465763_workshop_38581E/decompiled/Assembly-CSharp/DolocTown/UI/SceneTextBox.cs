using DolocTown.Config.Localization;
using DolocTown.Config.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class SceneTextBox : SceneBoxBase
{
	[SerializeField]
	private Text textTitle;

	[SerializeField]
	private Text textContent;

	[SerializeField]
	private Text textSignature;

	[SerializeField]
	private float minWidth = 160f;

	[SerializeField]
	private float maxWidth = 750f;

	[SerializeField]
	private LayoutElement[] layoutElements;

	public void Render(SceneTextBoxArgs sceneTextArgs)
	{
		Render(sceneTextArgs.Title, sceneTextArgs.Content, sceneTextArgs.Signature);
	}

	public void Render(AlignmentText title, AlignmentText content, AlignmentText signature)
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
