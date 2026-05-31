using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EquipmentViewer : CraftViewer<EquipmentData>
{
	[SerializeField]
	private DolocNavigationButton collect;

	[SerializeField]
	private Text item_count;

	[SerializeField]
	private Text unlock_tip;

	[SerializeField]
	private Text electronic_tip;

	[SerializeField]
	private Text type_desc;

	public DolocNavigationButton BtnCollect => collect;

	protected override void __Init()
	{
		base.__Init();
		collect.Init();
	}

	protected override string GetEmptyInfo()
	{
		return base.staticTexts.EquipmentViewerEmpty;
	}

	protected override void OnShow(EquipmentData data)
	{
		contentCanvas.blocksRaycasts = data.showCompleteInfo;
		if (!data.showCompleteInfo)
		{
			contentCanvas.alpha = 0f;
			emptyInfo.text = base.staticTexts.EquipmentPanelHide;
			emptyInfo.gameObject.SetActive(value: true);
			return;
		}
		base.OnShow(data);
		btnCraft.grayed = !data.isUnlock || !data.isCostEnough;
		collect.iconSprite = data.collectIcon;
		item_count.text = data.itemCount.ToString();
		unlock_tip.text = data.getUnLockTipText;
		electronic_tip.text = data.electronicTip;
		type_desc.text = data.subType;
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentCanvas.GetComponent<RectTransform>());
	}
}
