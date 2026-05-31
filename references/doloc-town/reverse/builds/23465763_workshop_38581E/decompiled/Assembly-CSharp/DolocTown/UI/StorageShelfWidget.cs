using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public class StorageShelfWidget : AutoSizeUIPanel, INavPanel
{
	private BoxInventoryWidget boxTemplate;

	[SerializeField]
	private AutoSizeText title;

	[HideInInspector]
	public List<BoxInventoryWidget> boxUis = new List<BoxInventoryWidget>();

	private List<BoxInventoryWidget> boxPool = new List<BoxInventoryWidget>();

	private GridLayoutGroup layoutGroup;

	private int lineCapacity;

	[SerializeField]
	public float distanceWeight = 10f;

	[SerializeField]
	public float angleLimit = 90f;

	public int boxCount => boxUis.Count;

	public int boxCapacity { get; private set; }

	private int rowCount => Mathf.CeilToInt((float)boxCount / (float)lineCapacity);

	private int boxSelectableCount => boxTemplate.allSelectableCount;

	public Selectable[] allSelectablesArray
	{
		get
		{
			Selectable[] array = new Selectable[boxCount * boxSelectableCount];
			int num = 0;
			foreach (BoxInventoryWidget item in boxPool)
			{
				Selectable[] array2 = item.allSelectablesArray;
				foreach (Selectable selectable in array2)
				{
					array[num++] = selectable;
				}
			}
			return array;
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		BoxInventoryWidget[] componentsInChildren = base.transform.GetComponentsInChildren<BoxInventoryWidget>();
		boxTemplate = componentsInChildren[0];
		BoxInventoryWidget[] array = componentsInChildren;
		foreach (BoxInventoryWidget boxInventoryWidget in array)
		{
			boxPool.Add(boxInventoryWidget);
			boxInventoryWidget.Init();
		}
		layoutGroup = GetComponent<GridLayoutGroup>();
		DolocAssert.IsTrue(layoutGroup != null);
		lineCapacity = layoutGroup.constraintCount;
		boxCapacity = boxTemplate.transform.GetComponentsInChildren<ItemNavSlot>().Length - 1;
		SetCapacity(6, 5);
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
	}

	public void SetCapacity(int boxCount, int boxCapacity)
	{
		if (this.boxCount != boxCount)
		{
			CheckBoxCount(boxCount);
		}
		if (this.boxCapacity != boxCapacity)
		{
			this.boxCapacity = boxCapacity;
			for (int i = 0; i < boxCount; i++)
			{
				boxUis[i].SetCapacity(boxCapacity, boxCapacity);
			}
		}
		RebuildLayout();
	}

	private void CheckBoxCount(int count)
	{
		int num = count - boxPool.Count;
		for (int i = 0; i < num; i++)
		{
			BoxInventoryWidget component = Object.Instantiate(boxTemplate, base.transform).GetComponent<BoxInventoryWidget>();
			component.Init();
			boxPool.Add(component);
		}
		for (int j = 0; j < boxPool.Count; j++)
		{
			boxPool[j].gameObject.SetActive(value: false);
		}
		boxUis.Clear();
		for (int k = 0; k < count; k++)
		{
			BoxInventoryWidget boxInventoryWidget = boxPool[k];
			boxUis.Add(boxInventoryWidget);
			boxInventoryWidget.gameObject.SetActive(value: true);
		}
	}

	public void RemoveBoxContentReceivers()
	{
		for (int i = 0; i < boxCount; i++)
		{
			boxUis[i].UnBindBox();
		}
	}

	public void ForEach(UnityAction<int, BoxInventoryWidget> call)
	{
		for (int i = 0; i < boxCount; i++)
		{
			call?.Invoke(i, boxUis[i]);
		}
	}

	public void SetTitle(string text)
	{
		title.text = text;
	}

	public void RefreshView(LinearInventory inventory)
	{
		ItemBox[] array = (from x in inventory.ReadAllWithNull()
			select x as ItemBox).ToArray();
		for (int i = 0; i < Mathf.Min(array.Length, boxUis.Count); i++)
		{
			ItemBox itemBox = array[i];
			BoxInventoryWidget boxInventoryWidget = boxUis[i];
			boxInventoryWidget.UnBindBox();
			if (itemBox != null)
			{
				boxInventoryWidget.BindBox(itemBox);
			}
		}
	}
}
