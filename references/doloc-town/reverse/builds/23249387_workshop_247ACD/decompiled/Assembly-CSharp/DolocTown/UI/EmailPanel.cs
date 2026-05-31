using UnityEngine;

namespace DolocTown.UI;

public class EmailPanel : DolocPagedLinearUI<EmailSlot, EmailData>
{
	[SerializeField]
	public IconWithTitleMenu subMenu;

	[SerializeField]
	public EmailViewer emailViewer;

	public IScrollContentRect contentRect => emailViewer;

	protected override SlotLayout layout => SlotLayout.Vertical;

	protected override void __Init()
	{
		base.__Init();
		emailViewer.Init();
		onDataSelect.AddListener(RenderViewer);
	}

	protected override EmailSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<EmailSlot>(includeInactive: true);
	}

	protected override void RenderSlot(EmailSlot slot, EmailData data)
	{
		slot.Render(data);
	}

	protected override void OnInitSlot(EmailSlot slot)
	{
	}

	private void RenderViewer(int index)
	{
		EmailSlot slot = GetSlot(index);
		TryGetData(slot.index, out var data);
		emailViewer.Render(data);
		RebuildLayout();
	}
}
