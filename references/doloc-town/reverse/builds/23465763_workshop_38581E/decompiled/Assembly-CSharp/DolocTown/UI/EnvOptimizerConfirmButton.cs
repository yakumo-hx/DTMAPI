using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EnvOptimizerConfirmButton : DolocNavigationButton
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtSubTitle;

	[SerializeField]
	private ProgressBar progressBar;

	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Color highlightedColor;

	public void SetText(string text, string subText = "")
	{
		if (text.IsNullOrEmpty())
		{
			SetVisible(value: false);
			return;
		}
		txtTitle.text = text;
		SetSubText(subText);
	}

	public void SetSubText(string subText)
	{
		SetText(txtSubTitle, subText);
	}

	public void SetProgress(float progress, bool setPercentage)
	{
		if (setPercentage)
		{
			progressBar.SetProgress(progress, $"{progress * 100f:F0}%");
		}
		else
		{
			progressBar.SetProgress(progress);
		}
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highlightedColor : normalColor);
	}
}
