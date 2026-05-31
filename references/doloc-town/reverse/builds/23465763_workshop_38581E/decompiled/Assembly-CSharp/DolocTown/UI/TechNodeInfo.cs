using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechNodeInfo : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text desc;

	[SerializeField]
	private Text cost;

	public void Render(TechNodeInfoData data)
	{
		base.iconSprite = data.Icon;
		title.text = data.Title;
		desc.text = data.Description;
		cost.text = data.CostInfo;
	}
}
