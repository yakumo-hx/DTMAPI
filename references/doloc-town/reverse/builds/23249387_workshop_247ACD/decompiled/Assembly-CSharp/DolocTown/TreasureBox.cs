using UnityEngine;

namespace DolocTown;

public class TreasureBox : ContainerObject
{
	public override int totalCapacity => itemList.countItems.Length;

	public override int lineCapacity => itemList.countItems.Length;

	protected override void OnLoadData(Room room)
	{
		base.OnLoadData(room);
		GameObject obj = base.gameObject;
		LinearInventory linearInventory = base.inventory;
		obj.SetActive(linearInventory != null && !linearInventory.isEmpty);
	}

	private void RefreshContent()
	{
		Item[] items = itemList.GenerateItems();
		base.inventory?.Overwrite(items, shouldEmit: true);
		SaveInventory();
	}

	protected override void OpenBox()
	{
		string itemListId = itemList.ItemListId;
		if (DolocAPI.GetEventTriggerCount(GameEventType.OPEN_TREASURE_BOX, itemListId) == 0)
		{
			RefreshContent();
			DolocAPI.BroadcastString(GameEventType.OPEN_TREASURE_BOX, itemList.ItemListId);
		}
		DolocAPI.EnterUI((DungeonCaseUiState state) => state.HandleStartUpArgs(this, SaveInventory));
	}

	protected override void SaveInventory()
	{
		base.SaveInventory();
		if (base.inventory == null || base.inventory.isEmpty)
		{
			_collider.enabled = false;
			DolocAPI.Delay(0.3f, delegate
			{
				base.gameObject.SetActive(value: false);
				Vector3 vector = base.transform.position;
				DolocAPI.effectProvider.RaiseInstPS(new Vector2(vector.x, vector.y + 0.5f), InstantParticleEffectsType.CRATE_CRACK_SMALL);
				_collider.enabled = true;
				HideTip();
			});
		}
	}

	public override bool ContentFilter(Item content)
	{
		return false;
	}
}
