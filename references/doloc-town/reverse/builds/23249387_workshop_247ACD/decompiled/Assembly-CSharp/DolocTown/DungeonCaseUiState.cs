namespace DolocTown;

public class DungeonCaseUiState : ContainerBaseUiState
{
	protected override bool singleOption => true;

	protected override SoundEvents SoundEventShow => SoundEvents.PLAY_CHARACTER_OPEN_TREASURE_BOX;

	protected override SoundEvents SoundEventHide
	{
		get
		{
			if (!base.containerInventory.isEmpty)
			{
				return SoundEvents.PLAY_UI_POP_DOWN;
			}
			return SoundEvents.PLAY_TREASURE_BOX_BROKEN;
		}
	}

	protected override bool disablePutMax => true;

	protected override bool disableUpdateTip => true;

	protected override void Sort()
	{
		base.backpackInventory.Sort();
	}

	protected override void PutAll()
	{
		for (int i = 0; i < base.containerInventory.capacity; i++)
		{
			Item item = base.containerInventory.Read(i);
			if (base.backpackInventory.CanPlaceIn(item))
			{
				ObtainItem(item);
				base.backpackInventory.PlaceItem(base.containerInventory.Take(i));
			}
		}
		CheckEmpty();
	}

	protected override void OnBackpackItemRender(int index, Item item)
	{
	}

	protected override void OnContainerItemClick(int index)
	{
		if (base.buffer.CurrentItem != null)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.InventoryPanelCannotPutIn);
		}
		else if (base.backpackInventory.CanPlaceIn(base.selectedItem))
		{
			ObtainItem(base.selectedItem);
			base.backpackInventory.PlaceItem(base.containerInventory.Take(base.currentIndex));
			CheckEmpty();
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
		}
	}

	private static void ObtainItem(Item item)
	{
		if (item != null)
		{
			for (int i = 0; i < item.count; i++)
			{
				DolocAPI.BroadcastString(GameEventType.OBTAIN_ITEM, item.name);
			}
		}
	}

	private void CheckEmpty()
	{
		if (base.containerInventory.isEmpty)
		{
			gameController.PopState();
		}
	}

	protected override string[] GetTipInBackpack()
	{
		return new string[2]
		{
			base.staticTexts.UiTipTakeOutAll,
			base.staticTexts.UiTipDestroyItem
		};
	}
}
