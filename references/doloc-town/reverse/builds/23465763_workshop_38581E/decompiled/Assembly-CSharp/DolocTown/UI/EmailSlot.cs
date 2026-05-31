using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EmailSlot : DolocNavigationButton
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtSender;

	[SerializeField]
	private GameObject newHint;

	[SerializeField]
	private GameObject collect;

	[SerializeField]
	private GameObject attachment;

	public void Render(EmailData data)
	{
		base.iconSprite = ((data.isNew || !data.isAccept) ? data.closedEmailSprite : data.openEmailSprite);
		newHint.SetActive(data.isNew);
		collect.SetActive(data.isCollect);
		attachment.SetActive(data.hasUnReceivedReward);
		txtTitle.text = data.title;
		txtSender.text = data.sender;
	}
}
