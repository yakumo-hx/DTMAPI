using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechPointRenderer : DolocNavigationButton
{
	[SerializeField]
	private Text number;

	[SerializeField]
	private Image bar;

	public void Render(TechPointSimpleData data)
	{
		base.iconSprite = data.icon;
		number.text = data.count;
		Vector3 localScale = bar.rectTransform.localScale;
		localScale.x = Mathf.Clamp(data.progress, 0f, 1f);
		bar.rectTransform.localScale = localScale;
	}
}
