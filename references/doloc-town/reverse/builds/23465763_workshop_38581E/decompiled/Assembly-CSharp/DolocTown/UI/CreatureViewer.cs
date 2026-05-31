using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CreatureViewer : DolocUIPanel
{
	[SerializeField]
	private AnimalDetailInfo animalDetailInfo;

	[SerializeField]
	private FishDetailInfo fishDetailInfo;

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
		animalDetailInfo.Init();
		fishDetailInfo.Init();
	}

	public void Render(CreatureDetailData data)
	{
		bool flag = data.display && data.isVisited;
		contentGroup.alpha = (flag ? 1 : 0);
		emptyHint.text = (data.display ? base.staticTexts.CollectionPanelMonsterUnknown : base.staticTexts.UiTipContentLock);
		emptyHint.gameObject.SetActive(!flag);
		if (flag)
		{
			animalDetailInfo.SetVisible(data.isAnimal);
			fishDetailInfo.SetVisible(!data.isAnimal);
			if (data.isAnimal)
			{
				animalDetailInfo.Render(data.animalData);
			}
			else
			{
				fishDetailInfo.Render(data.fishData);
			}
			contentTitle.text = base.staticTexts.CollectionPanelMonsterContentTitle;
			content.text = data.documentContent;
			rect.verticalNormalizedPosition = 1f;
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.rectTransform);
		}
	}
}
