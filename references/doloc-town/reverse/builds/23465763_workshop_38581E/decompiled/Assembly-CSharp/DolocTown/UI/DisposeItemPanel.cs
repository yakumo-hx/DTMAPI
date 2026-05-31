using DolocTown.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class DisposeItemPanel : DolocUiEntity, IPointerClickHandler, IEventSystemHandler
{
	private Image image;

	protected override void __Init()
	{
		base.__Init();
		image = GetComponent<Image>();
		image.raycastTarget = false;
		DolocAPI.OnAfterLoadArchiveData.AddListener(Refresh);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		IDolocGameState currentState = DolocAPI.userInput.CurrentState;
		if (currentState is NormalGameState || currentState is EquipmentBarUiState)
		{
			if (!DolocAPI.IsItemDisposable(DolocAPI.archiveHandle.InventorySystem.buffer.CurrentItem))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrNotDisposeItem);
			}
			else
			{
				DolocAPI.DisposeBufferItem();
			}
		}
	}

	private void Refresh(bool isNewGame)
	{
		DolocAPI.archiveHandle.InventorySystem.buffer.onValueChanged.AddListener(delegate(Item item)
		{
			image.raycastTarget = item != null;
		});
	}
}
