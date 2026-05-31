using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AutomateBotSlot : DolocNavigationButton
{
	[SerializeField]
	private Image power;

	[SerializeField]
	private Image running;

	[SerializeField]
	private Image pause;

	[SerializeField]
	private Image empty;

	[SerializeField]
	private CanvasGroup content;

	[SerializeField]
	public Color normalColor;

	[SerializeField]
	public Color highLightColor;

	[SerializeField]
	public Color unplacedColor;

	public bool Empty => content.alpha == 0f;

	public void Render(Sprite icon, bool isLowPower, bool isPause)
	{
		power.gameObject.SetActive(isLowPower);
		base.iconSprite = icon;
		running.gameObject.SetActive(!isPause);
		pause.gameObject.SetActive(isPause);
		content.alpha = 1f;
		base.backgroundColor = normalColor;
		empty.gameObject.SetActive(value: false);
	}

	public void RenderEmpty()
	{
		empty.gameObject.SetActive(value: true);
		content.alpha = 0f;
		base.backgroundColor = unplacedColor;
	}
}
