using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AnimalPanel : DolocPagedLinearUI<AnimalSlot, AnimalFullInfoData>
{
	[SerializeField]
	protected AnimalViewer viewer;

	[SerializeField]
	protected Color normalTextColor;

	[SerializeField]
	protected Color selectedTextColor;

	[SerializeField]
	protected Text capacityInfo;

	protected override SlotLayout layout => SlotLayout.Vertical;

	public DolocNavigationButton CallButton => viewer.callButton;

	public DolocButtonComponent RenameButton => viewer.renameButton;

	public AnimalViewer Viewer => viewer;

	protected override void __Init()
	{
		base.__Init();
		viewer.Init();
	}

	protected override AnimalSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<AnimalSlot>(includeInactive: true);
	}

	protected override void OnInitSlot(AnimalSlot slot)
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

	protected override void RenderSlot(AnimalSlot slot, AnimalFullInfoData data)
	{
		slot.Render(data);
	}

	public void SetCapacityInfo(string info)
	{
		SetText(capacityInfo, info);
	}

	private void RefreshViewer(int index)
	{
		if (base.currentDatas.IsNullOrEmpty())
		{
			viewer.SetEmpty(value: true, base.staticTexts.UiAnimalNoAnimal);
			return;
		}
		if (index >= base.currentDatas.Length)
		{
			viewer.Hide();
			return;
		}
		AnimalFullInfoData animalFullInfoData = base.currentDatas[index];
		if (animalFullInfoData.notEmpty)
		{
			viewer.Show(animalFullInfoData);
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
}
