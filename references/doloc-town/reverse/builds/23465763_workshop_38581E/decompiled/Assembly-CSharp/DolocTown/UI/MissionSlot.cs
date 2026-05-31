using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MissionSlot : DolocNavigationButton
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtTip;

	[SerializeField]
	private Image typeIcon;

	[SerializeField]
	private Image completeIcon;

	public void Render(MissionData data)
	{
		txtTitle.text = data.title;
		SetText(txtTip, data.tip);
		SetSprite(typeIcon, data.typeIcon);
		completeIcon.gameObject.SetActive(data.isComplete);
	}
}
