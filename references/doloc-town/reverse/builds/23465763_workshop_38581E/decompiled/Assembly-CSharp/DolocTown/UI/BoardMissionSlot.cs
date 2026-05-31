using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BoardMissionSlot : DolocNavigationButton
{
	[SerializeField]
	private GameObject content;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text type;

	[SerializeField]
	private Text desc;

	[SerializeField]
	private Text sender;

	[SerializeField]
	private Text tip;

	[SerializeField]
	private RewardViewer rewardViewer;

	[SerializeField]
	public DolocButtonComponent confirmBtn;

	[SerializeField]
	private DeviceDetectTextCom btnText;

	[SerializeField]
	private GameObject emptyHint;

	[SerializeField]
	private Text emptyHintText;

	protected override void __Init()
	{
		base.__Init();
		rewardViewer.Init();
	}

	public void Render(BoardMissionData data)
	{
		content.SetActive(data.notEmpty);
		emptyHint.SetActive(!data.notEmpty);
		emptyHintText.text = DolocConfig.StaticTexts.BoardMissionEmpty;
		if (data.notEmpty)
		{
			title.text = data.title;
			type.text = data.type;
			desc.text = data.desc;
			sender.text = "——" + data.sender;
			tip.text = data.tip;
			rewardViewer.Render(data.rewardData);
			btnText.Text = (data.levelEnough ? DolocConfig.StaticTexts.UiEmailAcceptMission : DolocConfig.StaticTexts.BoardMissionLowLevel);
			confirmBtn.image.color = (data.levelEnough ? DolocUiColor.SLIENTCOLOR_PURPLE : DolocUiColor.SLIENTCOLOR_GREY);
		}
	}
}
