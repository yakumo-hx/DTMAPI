using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FishingProgressBar : DolocUiObject
{
	[SerializeField]
	private int fixedWidth;

	[SerializeField]
	private Image progressMask;

	public void SetProgress(float progress)
	{
		float x = (float)fixedWidth + (base.width - (float)fixedWidth) * (1f - progress);
		float y = progressMask.rectTransform.rect.height;
		progressMask.rectTransform.sizeDelta = new Vector2(x, y);
	}
}
