using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DroneWidget : AutoSizeUIPanel, INavPanel
{
	[SerializeField]
	private Text title;

	[FormerlySerializedAs("structItem")]
	[SerializeField]
	public DroneItemSlot structItemSlot;

	[SerializeField]
	private Transform cmpRoot;

	[FormerlySerializedAs("cmpItems")]
	public List<DroneItemSlot> cmpItemSlots;

	[SerializeField]
	public float distanceWeight = 10f;

	[SerializeField]
	public float angleLimit = 90f;

	public Selectable[] allSelectablesArray
	{
		get
		{
			Selectable[] array = new Selectable[cmpItemSlots.Count + 1];
			int num = 0;
			array[num++] = structItemSlot.button;
			foreach (DroneItemSlot cmpItemSlot in cmpItemSlots)
			{
				array[num++] = cmpItemSlot.button;
			}
			return array;
		}
	}

	public int allSelectableCount => allSelectablesArray.Length;

	protected override void __Init()
	{
		base.__Init();
		structItemSlot.Init();
		InitComponentItems();
		base.displayAnimType = UiPanelDisplayAnimType.FromBottom;
		base.transform.SetAsFirstSibling();
	}

	private void InitComponentItems()
	{
		cmpItemSlots = cmpRoot.GetComponentsInChildren<DroneItemSlot>(includeInactive: true)?.ToList();
		if (cmpItemSlots == null)
		{
			cmpItemSlots = new List<DroneItemSlot>();
		}
		for (int i = 0; i < cmpItemSlots.Count; i++)
		{
			DroneItemSlot droneItemSlot = cmpItemSlots[i];
			droneItemSlot.index = i;
			droneItemSlot.Init();
		}
		CheckComponentItemCount(0);
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		title.text = base.staticTexts.DronePanelTitle;
	}

	public void CheckComponentItemCount(int count)
	{
		DolocAssert.IsTrue(count <= cmpItemSlots.Count);
		for (int i = 0; i < cmpItemSlots.Count; i++)
		{
			cmpItemSlots[i].Clear();
			cmpItemSlots[i].SetVisible(i < count);
		}
	}

	private bool CheckIndex(int index)
	{
		int num;
		if (index < cmpItemSlots.Count)
		{
			num = (cmpItemSlots[index].isVisible ? 1 : 0);
			if (num != 0)
			{
				goto IL_003a;
			}
		}
		else
		{
			num = 0;
		}
		Debug.LogError($"index: {index} 不合法");
		goto IL_003a;
		IL_003a:
		return (byte)num != 0;
	}

	public DroneItemSlot GetComponentItemSlot(int index)
	{
		if (!CheckIndex(index))
		{
			return null;
		}
		return cmpItemSlots[index];
	}

	public DroneItemSlot GetLastComponentItemSlot()
	{
		for (int num = cmpItemSlots.Count - 1; num >= 0; num--)
		{
			if (cmpItemSlots[num].isVisible)
			{
				return cmpItemSlots[num];
			}
		}
		return structItemSlot;
	}

	public void ForEachComponent(UnityAction<DroneItemSlot> call)
	{
		foreach (DroneItemSlot cmpItemSlot in cmpItemSlots)
		{
			call?.Invoke(cmpItemSlot);
		}
	}

	public void ForEachComponent(UnityAction<int, DroneItemSlot> call)
	{
		for (int i = 0; i < cmpItemSlots.Count; i++)
		{
			call?.Invoke(i, cmpItemSlots[i]);
		}
	}

	public bool CheckComponentsEmpty()
	{
		foreach (DroneItemSlot cmpItemSlot in cmpItemSlots)
		{
			if (cmpItemSlot.isVisible && !cmpItemSlot.isEmpty)
			{
				return false;
			}
		}
		return true;
	}
}
