using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EmailViewer : DolocUiObject, IScrollContentRect
{
	[SerializeField]
	private Text textTitle;

	[SerializeField]
	private Text textContent;

	[SerializeField]
	private Text textSender;

	[SerializeField]
	private Text textTime;

	[SerializeField]
	private Text rewardTitle;

	[SerializeField]
	private Text rewardInfo;

	[SerializeField]
	private RewardViewer rewardViewer;

	[SerializeField]
	public DolocButtonComponent confirmButton;

	[SerializeField]
	private DeviceDetectTextCom buttonText;

	[SerializeField]
	private Text hintText;

	[SerializeField]
	private ScrollRect _scrollRect;

	[SerializeField]
	private CanvasGroup content;

	[SerializeField]
	private Text emptyHint;

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		rewardViewer.Init();
	}

	public void Render(EmailData data)
	{
		content.blocksRaycasts = data.notEmpty;
		content.alpha = (data.notEmpty ? 1 : 0);
		SetText(emptyHint, data.notEmpty ? string.Empty : base.staticTexts.CollectionPanelItemUnknown);
		SetText(textTitle, data.title);
		SetText(textContent, data.content);
		SetText(textSender, data.sender);
		SetText(textTime, data.timeText);
		SetText(rewardInfo, data.rewardText, new Component[1] { rewardTitle });
		rewardViewer.SetVisible(data.hasReward);
		if (data.hasReward)
		{
			for (int i = 0; i < rewardViewer.Slots.Length; i++)
			{
				if (i < data.rewardStates.Length)
				{
					rewardViewer.Slots[i].alpha = (data.rewardStates[i].isAccept ? 0.3f : 1f);
					SetSprite(rewardViewer.Slots[i].iconImg, data.rewardStates[i].icon);
					rewardViewer.Slots[i].number = data.rewardStates[i].count;
				}
				rewardViewer.Slots[i].SetVisible(i < data.rewardStates.Length);
			}
		}
		if (!data.isAccept)
		{
			confirmButton.gameObject.SetActive(!string.IsNullOrEmpty(data.buttonText));
			if (confirmButton.gameObject.activeSelf)
			{
				buttonText.Text = data.buttonText;
			}
			hintText.gameObject.SetActive(value: false);
		}
		else
		{
			confirmButton.gameObject.SetActive(value: false);
			hintText.gameObject.SetActive(value: true);
			hintText.text = data.hintText;
		}
	}

	public void OnMove()
	{
	}
}
