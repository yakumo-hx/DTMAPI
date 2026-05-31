using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class SingleInventory
{
	[JsonProperty]
	private Item item;

	private List<SingleInventoryReceiver> receivers = new List<SingleInventoryReceiver>();

	public UnityEvent<Item> onValueChanged = new UnityEvent<Item>();

	public Item CurrentItem
	{
		get
		{
			return item;
		}
		set
		{
			if (item != value)
			{
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_PICK_UP_ITEM);
			}
			item = value;
			Update();
		}
	}

	public bool IsEmpty => item == null;

	public bool IsFull => item != null;

	public SingleInventory()
	{
		item = null;
	}

	[JsonConstructor]
	private SingleInventory(Item item)
	{
		this.item = item;
	}

	public void Update()
	{
		onValueChanged.Invoke(item);
		if (item == null)
		{
			foreach (SingleInventoryReceiver receiver in receivers)
			{
				receiver(null);
			}
			return;
		}
		foreach (SingleInventoryReceiver receiver2 in receivers)
		{
			receiver2(item);
		}
	}

	public void BindReceiver(SingleInventoryReceiver receiver)
	{
		if (!receivers.Contains(receiver))
		{
			receivers.Add(receiver);
			Update();
		}
	}

	public void UnbindReceiver(SingleInventoryReceiver receiver)
	{
		if (receivers.Contains(receiver))
		{
			receivers.Add(receiver);
		}
	}

	public void ClearReceivers()
	{
		receivers.Clear();
	}

	public Item Take()
	{
		if (item == null)
		{
			return null;
		}
		foreach (SingleInventoryReceiver receiver in receivers)
		{
			receiver(null);
		}
		Item result = item;
		CurrentItem = null;
		return result;
	}
}
