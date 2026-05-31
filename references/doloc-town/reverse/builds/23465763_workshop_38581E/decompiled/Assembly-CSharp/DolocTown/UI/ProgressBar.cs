using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ProgressBar : DolocNavigationButton
{
	[SerializeField]
	private bool useText;

	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtProgress;

	[SerializeField]
	private RectTransform progressBackground;

	[SerializeField]
	private Image progressMask;

	[SerializeField]
	private ProgressBarDirection directionType;

	public void SetProgress(float progress)
	{
		progress = Mathf.Clamp01(progress);
		float num = progressBackground.rect.width;
		float num2 = progressBackground.rect.height;
		switch (directionType)
		{
		case ProgressBarDirection.Horizontal:
			num *= progress;
			break;
		case ProgressBarDirection.Vertical:
			num2 *= progress;
			break;
		}
		progressMask.rectTransform.sizeDelta = new Vector2(num, num2);
	}

	public void SetProgress(float progress, string description)
	{
		SetProgress(progress);
		SetText(txtProgress, description);
	}

	public void SetTitle(string title)
	{
		SetText(txtTitle, title);
	}
}
