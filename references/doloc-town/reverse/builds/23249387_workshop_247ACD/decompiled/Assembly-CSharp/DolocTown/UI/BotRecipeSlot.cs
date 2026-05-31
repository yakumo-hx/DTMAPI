using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BotRecipeSlot : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text cost;

	[SerializeField]
	private GameObject line;

	[SerializeField]
	public Color normalColor;

	[SerializeField]
	public Color selectedColor;

	[SerializeField]
	public Color usedColor;

	public bool Using { get; private set; }

	public void Render(BotRecipeData data)
	{
		title.text = data.title;
		base.iconSprite = data.icon;
		cost.text = data.costInfo;
		line.SetActive(!data.isLastLine);
		Using = data.used;
	}
}
