using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class QuantitySubmitPanelBase<TData> : AutoSizeUIPanel where TData : QuantitySubmitData
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtInfo;

	[SerializeField]
	private QuantitySubmitBar quantitySubmitBar;

	[SerializeField]
	private DolocButtonComponent btnConfirm;

	[SerializeField]
	private DolocButtonComponent btnCancel;

	[HideInInspector]
	public UnityEvent<int> onConfirm = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent onCancel = new UnityEvent();

	protected TData currentData;

	public int currentCount => quantitySubmitBar.currentCount;

	protected override void __Init()
	{
		base.__Init();
		quantitySubmitBar.Init();
		quantitySubmitBar.onCurrentCountChange.AddListener(RefreshView);
		btnConfirm.onClick.AddListener(Confirm);
		btnCancel.onClick.AddListener(Cancel);
	}

	private void Confirm()
	{
		onConfirm.Invoke(currentCount);
	}

	private void Cancel()
	{
		onCancel.Invoke();
	}

	public virtual void Render(TData data)
	{
		SetTitle(data.title);
		SetInfo(data.info);
		currentData = data;
		quantitySubmitBar.Render(data);
	}

	protected void SetTitle(string text)
	{
		SetText(txtTitle, text);
	}

	protected void SetInfo(string text)
	{
		SetText(txtInfo, text);
	}

	protected virtual void RefreshView(int count)
	{
	}

	public void SetMin()
	{
		quantitySubmitBar.SetMin();
	}

	public void SetMax()
	{
		quantitySubmitBar.SetMax();
	}

	public bool AddDiff(int diff)
	{
		return quantitySubmitBar.AddDiff(diff);
	}
}
