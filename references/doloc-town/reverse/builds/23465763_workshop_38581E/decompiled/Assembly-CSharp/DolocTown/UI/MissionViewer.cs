using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class MissionViewer : DolocUiObject, IScrollContentRect
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtType;

	[SerializeField]
	private Text txtSender;

	[SerializeField]
	private Text txtDesc;

	[SerializeField]
	private Text txtAppendInfo;

	[SerializeField]
	private Text txtAppend;

	[SerializeField]
	private Text txtTip;

	[SerializeField]
	private Image imgTip;

	[SerializeField]
	private Text txtTimeInfo;

	[SerializeField]
	private Text txtTime;

	[SerializeField]
	private Text rewardTitle;

	[SerializeField]
	private Text rewardInfo;

	[SerializeField]
	private RewardViewer rewardViewer;

	[SerializeField]
	private PositionViewer positionViewer;

	[SerializeField]
	private GameObject completeInfo;

	[SerializeField]
	private ScrollRect _scrollRect;

	public ScrollRect scrollRect => _scrollRect;

	public float moveDelta => 0.05f;

	protected override void __Init()
	{
		base.__Init();
		rewardViewer.Init();
		positionViewer.Init();
	}

	public void Render(MissionData data)
	{
		txtTitle.text = data.title;
		SetText(txtSender, data.sender);
		SetText(txtType, data.type);
		SetText(txtDesc, data.description);
		SetText(txtAppend, data.appendix, new Component[1] { txtAppendInfo });
		SetText(txtTip, data.tipWithStatus);
		SetSprite(imgTip, data.previewImage, autoSize: true);
		SetText(txtTime, data.timeLimit, new Component[1] { txtTimeInfo });
		rewardTitle.gameObject.SetActive(data.rewardData.notEmpty);
		rewardInfo.gameObject.SetActive(data.rewardData.notEmpty);
		rewardViewer.Render(data.rewardData);
		positionViewer.Render(data.mapTipData);
		completeInfo.SetActive(data.isComplete);
		RebuildLayout();
		scrollRect.verticalScrollbar.value = 1f;
	}

	public void ClickMapButton()
	{
		positionViewer.button.FireClick();
	}

	public void OnMove()
	{
	}
}
