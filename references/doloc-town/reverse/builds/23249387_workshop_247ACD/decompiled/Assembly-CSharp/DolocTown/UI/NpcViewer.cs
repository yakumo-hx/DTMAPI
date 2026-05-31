using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class NpcViewer : DolocUIPanel
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Text npcName;

	[SerializeField]
	private Image giftIcon;

	[SerializeField]
	private Text address;

	[SerializeField]
	private FavorabilityHeartViewer heartViewer;

	[SerializeField]
	private Text likingHint;

	[SerializeField]
	private Text missionHint;

	[SerializeField]
	private Text likeContent;

	[SerializeField]
	private Text contentTitle;

	[SerializeField]
	private Text content;

	[SerializeField]
	private VerticalLayoutGroup bottom;

	[SerializeField]
	private RectTransform infoRoot;

	[SerializeField]
	private CanvasGroup contentGroup;

	[SerializeField]
	public ScrollRect rect;

	[SerializeField]
	private Text emptyHint;

	[SerializeField]
	private Sprite[] giftSprites;

	protected override void __Init()
	{
		base.__Init();
		heartViewer.Init();
	}

	public void Render(NpcDetailData data)
	{
		bool flag = data.display && data.isVisited;
		contentGroup.alpha = (flag ? 1 : 0);
		emptyHint.text = (data.display ? base.staticTexts.CollectionPanelItemUnknown : base.staticTexts.UiTipContentLock);
		emptyHint.gameObject.SetActive(!flag);
		if (flag)
		{
			SetSprite(icon, data.icon, autoSize: true);
			npcName.text = data.name;
			address.text = data.address;
			giftIcon.gameObject.SetActive(data.unLockGift);
			Image image = giftIcon;
			image.sprite = data.giftCount switch
			{
				0 => giftSprites[0], 
				1 => giftSprites[1], 
				_ => giftSprites[2], 
			};
			heartViewer.Render(data.likingMaxLevel, data.likingLevel);
			heartViewer.SetVisible(data.unLockGift);
			likingHint.text = base.staticTexts.CollectionPanelNpcLikingLvLock;
			likingHint.gameObject.SetActive(data.showLikingLvHint);
			missionHint.text = data.missionHint;
			missionHint.gameObject.SetActive(!data.missionHint.IsNullOrEmpty());
			likeContent.text = (data.unLockGift ? data.likeContent : base.staticTexts.CollectionPanelNpcLikingLock.Colored(DolocUiColor.TEXTCOLOR_CYAN));
			contentTitle.text = base.staticTexts.CollectionPanelNpcContentTitle;
			content.text = data.documentContent;
			rect.verticalNormalizedPosition = 1f;
			LayoutRebuilder.ForceRebuildLayoutImmediate(infoRoot);
			float num = ((RectTransform)bottom.transform).rect.height;
			likeContent.GetComponent<LayoutElement>().preferredHeight = Mathf.Min(likeContent.preferredHeight, num - address.preferredHeight - bottom.spacing);
		}
	}
}
