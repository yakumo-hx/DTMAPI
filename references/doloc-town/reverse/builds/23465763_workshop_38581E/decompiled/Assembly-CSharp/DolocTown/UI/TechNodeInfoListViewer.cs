using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechNodeInfoListViewer : DolocScrollGridUI<TechNodeInfo, TechNodeInfoData>
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_TECH_NODE_SLOT);

	protected override void RenderSlot(TechNodeInfo slot, TechNodeInfoData data)
	{
		slot.Render(data);
	}

	public override void BuildNavigation()
	{
	}

	public void SetVerticalScrollbar(float delta)
	{
		if (!(contentRect.rect.height <= _scrollRect.viewport.rect.height))
		{
			Scrollbar verticalScrollbar = _scrollRect.verticalScrollbar;
			verticalScrollbar.value = Mathf.Clamp01(verticalScrollbar.value + delta * 0.05f);
		}
	}
}
