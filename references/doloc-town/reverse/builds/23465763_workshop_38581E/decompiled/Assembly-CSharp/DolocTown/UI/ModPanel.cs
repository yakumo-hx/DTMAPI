using System;
using UnityEngine;

namespace DolocTown.UI;

public class ModPanel : DolocPagedLinearUI<ModSlot, ModData>
{
	[SerializeField]
	public IconWithTitleMenu subMenu;

	[SerializeField]
	public ModViewer modViewer;

	public Action<int> onToggleSwitch;

	public Action<int> onMoveUp;

	public Action<int> onMoveDown;

	public IScrollContentRect contentRect => modViewer;

	protected override SlotLayout layout => SlotLayout.Vertical;

	protected override void __Init()
	{
		base.__Init();
		modViewer.Init();
		onDataSelect.AddListener(RenderViewer);
		emptyInfo.text = string.Empty;
	}

	protected override ModSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<ModSlot>(includeInactive: true);
	}

	protected override void RenderSlot(ModSlot slot, ModData data)
	{
		slot.onToggleSwitch = onToggleSwitch;
		slot.onMoveUp = onMoveUp;
		slot.onMoveDown = onMoveDown;
		slot.Render(data);
	}

	protected override void OnInitSlot(ModSlot slot)
	{
	}

	private void RenderViewer(int index)
	{
		ModSlot slot = GetSlot(index);
		TryGetData(slot.index, out var data);
		modViewer.Render(data);
		RebuildLayout();
	}

	public void RefreshNavigation(ModSlot slot)
	{
		if (!(slot == null))
		{
			modViewer.RefreshNavigation();
			slot.button.SetNavigationOnRight(modViewer.switchButton.button);
			modViewer.switchButton.button.SetNavigationOnLeft(slot.button);
			modViewer.openLocalButton.button.SetNavigationOnLeft(slot.button);
			modViewer.workshopButton.button.SetNavigationOnLeft(slot.button);
			modViewer.workshopHomepageButton.button.SetNavigationOnLeft(slot.button);
		}
	}

	public void SetEmpty(bool value)
	{
		emptyInfo.text = (value ? base.staticTexts.UiModNoMod : string.Empty);
		modViewer.SetEmpty(value);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		modViewer.openLocalButton.buttonText = base.staticTexts.UiModOpenLocalDirectory;
		modViewer.workshopButton.buttonText = base.staticTexts.UiModOpenWorkshop;
	}
}
