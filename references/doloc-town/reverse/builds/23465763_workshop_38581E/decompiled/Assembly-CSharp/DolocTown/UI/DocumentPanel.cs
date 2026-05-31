using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DocumentPanel : DolocPagedLinearUI<DocumentSlot, DocumentData>
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtDesc;

	[SerializeField]
	private Color normalTextColor;

	[SerializeField]
	private Color selectedTextColor;

	[SerializeField]
	private DocumentViewer viewer;

	public IScrollContentRect contentRect => viewer;

	public string title
	{
		get
		{
			return txtTitle.text;
		}
		set
		{
			txtTitle.text = value;
		}
	}

	public string description
	{
		get
		{
			return txtDesc.text;
		}
		set
		{
			txtDesc.text = value;
		}
	}

	public DocumentViewer Viewer => viewer;

	protected override SlotLayout layout => SlotLayout.Vertical;

	protected override void __Init()
	{
		base.__Init();
		viewer.Init();
	}

	protected override DocumentSlot[] GetSlots()
	{
		return slotsRoot.GetComponentsInChildren<DocumentSlot>(includeInactive: true);
	}

	protected override void OnInitSlot(DocumentSlot slot)
	{
		slot.onSelect.AddListener(RefreshDocViewer);
		slot.onSelect.AddListener(delegate
		{
			slot.txtColor = selectedTextColor;
		});
		slot.onDeselect.AddListener(delegate
		{
			slot.txtColor = normalTextColor;
		});
	}

	protected override void RenderSlot(DocumentSlot slot, DocumentData data)
	{
		slot.title = data.title;
	}

	protected override void OnRefreshView()
	{
		base.OnRefreshView();
		RefreshDocViewer(currentSlotIndex);
	}

	private void RefreshDocViewer(int index)
	{
		if (index >= base.currentDatas.Length)
		{
			viewer.Hide();
			return;
		}
		DocumentData documentData = base.currentDatas[index];
		if (documentData.notEmpty)
		{
			viewer.Render(documentData.title, documentData.author, documentData.content);
		}
		else
		{
			viewer.Hide();
		}
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		DocumentSlot[] array = base.slots;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].highLighted = false;
		}
	}
}
