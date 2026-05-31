using System;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown.UI;

public abstract class DolocSinglePageUI<TData> : DolocUiObject, ISinglePageUI<TData> where TData : IUIData
{
	protected TData currentData;

	[SerializeField]
	protected PageInfo pageInfo;

	[HideInInspector]
	public UnityEvent<int> onSelect = new UnityEvent<int>();

	public Func<int, TData> DataGetter { get; set; }

	public int totalCapacity { get; private set; }

	public int currentIndex { get; set; }

	protected override void __Init()
	{
		base.__Init();
		pageInfo.Init();
		pageInfo.BindChangePageAction(PrevPage, NextPage);
	}

	public void SetTotalCapacity(int totalCapacity)
	{
		this.totalCapacity = totalCapacity;
	}

	private void HandlePageChanged(int newPageIndex)
	{
		onSelect.Invoke(newPageIndex);
		if (totalCapacity > 1)
		{
			int num = currentIndex;
			currentIndex = newPageIndex;
			if (num != currentIndex)
			{
				RefreshView();
			}
		}
	}

	public void PrevPage()
	{
		int newPageIndex = (currentIndex + totalCapacity - 1) % totalCapacity;
		HandlePageChanged(newPageIndex);
	}

	public void NextPage()
	{
		int newPageIndex = (currentIndex + 1) % totalCapacity;
		HandlePageChanged(newPageIndex);
	}

	public void RefreshView()
	{
		if (DataGetter == null)
		{
			Debug.LogError($"{GetType()}未绑定DattaGetter!");
			return;
		}
		currentData = DataGetter(currentIndex);
		if (currentData != null && currentData.notEmpty)
		{
			SetEmpty(value: false);
			Render(currentData);
		}
		else
		{
			Debug.LogWarning($"ui数据index: {currentIndex}为空");
			SetEmpty(value: true);
		}
		pageInfo.SetPage(currentIndex, totalCapacity);
		OnRefreshView();
	}

	protected abstract void Render(TData data);

	protected abstract void SetEmpty(bool value);

	public void SelectPage(int index)
	{
		if (index > totalCapacity)
		{
			Debug.LogWarning($"index{index}超出范围");
		}
		int newPageIndex = ((index != 0) ? (index % totalCapacity) : 0);
		HandlePageChanged(newPageIndex);
	}

	protected virtual void OnRefreshView()
	{
	}
}
