using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class GunReloadTip : DolocUiRecyclableObject
{
	[SerializeField]
	private Image imgBar;

	[SerializeField]
	private Image imgBackground;

	private float progressBarHeight;

	public float process
	{
		set
		{
			value = Mathf.Clamp01(value);
			imgBar.transform.localPosition = new Vector3(0f, progressBarHeight * value, 1f);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		progressBarHeight = imgBackground.rectTransform.sizeDelta.y - imgBar.rectTransform.sizeDelta.y;
	}
}
