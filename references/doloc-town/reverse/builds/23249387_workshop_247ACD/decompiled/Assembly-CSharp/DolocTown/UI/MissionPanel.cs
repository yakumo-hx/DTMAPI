using UnityEngine;

namespace DolocTown.UI;

public class MissionPanel : DolocPagedLinearUI<MissionSlot, MissionData>
{
	[SerializeField]
	public MissionViewer missionViewer;

	public IScrollContentRect contentRect => missionViewer;

	protected override SlotLayout layout => SlotLayout.Vertical;

	protected override void __Init()
	{
		base.__Init();
		missionViewer.Init();
		onDataSelect.AddListener(RenderViewer);
	}

	protected override MissionSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<MissionSlot>(includeInactive: true);
	}

	protected override void OnInitSlot(MissionSlot slot)
	{
	}

	protected override void RenderSlot(MissionSlot slot, MissionData data)
	{
		slot.Render(data);
	}

	private void RenderViewer(int index)
	{
		MissionSlot slot = GetSlot(index);
		if (!TryGetData(slot.index, out var data))
		{
			missionViewer.SetVisible(value: false);
		}
		missionViewer.SetVisible(value: true);
		missionViewer.Render(data);
		RebuildLayout();
	}

	protected override void OnRefreshView()
	{
		base.OnRefreshView();
		missionViewer.SetVisible(base.currentDatas.Length != 0);
	}
}
