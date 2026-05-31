using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

public class StorePanel : DolocUIPanel, IPageUI<StoreItemData>, IView
{
	[SerializeField]
	public BackpackSideBarWidget backpackPanel;

	[FormerlySerializedAs("storePanel")]
	[SerializeField]
	public StoreWidget storeWidget;

	public Func<int, int, StoreItemData[]> DataGetter
	{
		get
		{
			return storeWidget.DataGetter;
		}
		set
		{
			storeWidget.DataGetter = value;
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		backpackPanel.SetMode(BackpackSideBarMode.Store);
		SetLeftAndRightLayout(storeWidget, backpackPanel);
	}

	public void SetTotalCapacity(int totalCapacity)
	{
		storeWidget.SetTotalCapacity(totalCapacity);
	}

	public void NextPage()
	{
		storeWidget.NextPage();
	}

	public void PrevPage()
	{
		storeWidget.PrevPage();
	}

	public void RefreshView()
	{
		storeWidget.RefreshView();
	}

	public void Select(int index)
	{
		storeWidget.Select(index);
	}

	public void BuildNavigation()
	{
		Selectable[] array = storeWidget.allSelectablesArray.Concat(backpackPanel.allSelectablesArray).ToArray();
		if (array.Length != 0)
		{
			array.RebuildNavigationHorizontal(array);
			backpackPanel.allSelectablesArray.RebuildNavigationVertical(backpackPanel.allSelectablesArray);
		}
	}
}
