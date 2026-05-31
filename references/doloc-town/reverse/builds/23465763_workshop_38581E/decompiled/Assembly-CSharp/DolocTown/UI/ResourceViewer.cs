using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ResourceViewer : DolocUIPanel
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text count;

	[SerializeField]
	private Text habitat;

	[SerializeField]
	private Text growthPeriod;

	[SerializeField]
	private Text dropText;

	[SerializeField]
	private CostItemViewer costItemViewer;

	[SerializeField]
	private Text contentTitle;

	[SerializeField]
	private TextMeshProUGUI content;

	[SerializeField]
	private CanvasGroup contentGroup;

	[SerializeField]
	public ScrollRect rect;

	[SerializeField]
	private Text emptyHint;

	protected override void __Init()
	{
		base.__Init();
		costItemViewer.Init();
	}

	public void Render(ResourceDetailData data)
	{
		bool flag = data.display && data.isVisited;
		contentGroup.alpha = (flag ? 1 : 0);
		emptyHint.text = (data.display ? base.staticTexts.CollectionPanelMonsterUnknown : base.staticTexts.UiTipContentLock);
		emptyHint.gameObject.SetActive(!flag);
		if (flag)
		{
			SetSprite(icon, data.icon, autoSize: true);
			title.text = data.title;
			count.text = base.staticTexts.CollectionPanelResourceCollect.Format(data.collectCount);
			habitat.text = data.habitat;
			SetText(growthPeriod, data.growthPeriod);
			dropText.text = base.staticTexts.CollectionPanelResourceDropTitle;
			costItemViewer.Render(data.itemNames, data.dropItems, data.obtained);
			contentTitle.text = base.staticTexts.CollectionPanelMonsterContentTitle;
			content.text = data.documentContent;
			rect.verticalNormalizedPosition = 1f;
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.rectTransform);
		}
	}
}
