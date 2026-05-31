using DolocTown.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class DisposeItemWidget : DolocUiObject, IPointerClickHandler, IEventSystemHandler
{
	public void OnPointerClick(PointerEventData eventData)
	{
		Item currentItem = DolocAPI.archiveHandle.InventorySystem.buffer.CurrentItem;
		if (currentItem != null)
		{
			if (!DolocAPI.IsItemDisposable(currentItem))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiErrNotDisposeItem);
			}
			else
			{
				DolocAPI.DisposeBufferItem();
			}
		}
	}
}
