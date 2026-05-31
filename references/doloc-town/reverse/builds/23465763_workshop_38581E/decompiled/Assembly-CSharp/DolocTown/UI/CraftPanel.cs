using UnityEngine;

namespace DolocTown.UI;

public class CraftPanel<TSlot, TViewer, TData> : DolocPagedLinearUI<TSlot, TData>, ICraftPanel<TData>, IPageUI<TData>, IView where TSlot : CraftSlot<TData> where TViewer : CraftViewer<TData> where TData : ICraftData
{
	[SerializeField]
	protected TViewer viewer;

	[SerializeField]
	protected Color normalTextColor;

	[SerializeField]
	protected Color selectedTextColor;

	protected override SlotLayout layout => SlotLayout.Vertical;

	public ConfirmCraftButton BtnCraft => viewer.BtnCraft;

	public TViewer Viewer => viewer;

	protected override void __Init()
	{
		base.__Init();
		viewer.Init();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	protected override TSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<TSlot>(includeInactive: true);
	}

	protected override void OnInitSlot(TSlot slot)
	{
		slot.onSelect.AddListener(RefreshViewer);
		slot.onSelect.AddListener(delegate
		{
			slot.txtColor = selectedTextColor;
		});
		slot.onDeselect.AddListener(delegate
		{
			slot.txtColor = normalTextColor;
		});
	}

	protected override void RenderSlot(TSlot slot, TData data)
	{
		slot.Render(data);
	}

	private void RefreshViewer(int index)
	{
		if (base.currentDatas.IsNullOrEmpty())
		{
			viewer.SetEmpty(value: true);
			return;
		}
		if (index >= base.currentDatas.Length)
		{
			viewer.Hide();
			return;
		}
		TData data = base.currentDatas[index];
		if (data.notEmpty)
		{
			viewer.Show(data);
		}
		else
		{
			viewer.Hide();
		}
	}

	protected override void OnRefreshView()
	{
		base.OnRefreshView();
		RefreshViewer(currentSlotIndex);
	}

	public void RaiseCostItemsFadeUp()
	{
		viewer.RaiseCostItemsFadeUp();
	}
}
