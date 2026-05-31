using RedSaw;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechNodePreviewViewer : DolocUIPanel
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text nodeDesc;

	[SerializeField]
	private Text hint;

	[SerializeField]
	public TechNodeInfoListViewer listViewer;

	[SerializeField]
	public ConfirmCraftButton button;

	private ObjectPool<DolocIconWithText> techPointPool;

	protected override void __Init()
	{
		base.__Init();
		button.Init();
		techPointPool = new ObjectPool<DolocIconWithText>(DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_TECH_POINT_COST_SLOT), base.transform, usePreset: true);
	}

	public void Render(TechNodePreviewData data)
	{
		icon.sprite = data.icon;
		title.text = data.title;
		nodeDesc.text = data.desc;
		listViewer.RefreshView(data.nodeInfoData);
		techPointPool.CheckCount(data.pointCostData.Length);
		techPointPool.Sort();
		for (int i = 0; i < data.pointCostData.Length; i++)
		{
			techPointPool[i].iconSprite = data.pointCostData[i].icon;
			techPointPool[i].description = data.pointCostData[i].count;
			techPointPool[i].alpha = (data.pointCostData[i].grayed ? 0.3f : 1f);
		}
		button.buttonText = data.buttonText;
		button.grayed = data.buttonGrayed;
		listViewer.ResetNormalizedPosition();
	}

	protected override void OnStartShow()
	{
		hint.text = base.staticTexts.TechtreeNodeHint;
	}
}
