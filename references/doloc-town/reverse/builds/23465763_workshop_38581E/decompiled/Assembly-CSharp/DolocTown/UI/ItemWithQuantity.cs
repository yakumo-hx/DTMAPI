using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemWithQuantity : DolocUiObject
{
	[SerializeField]
	private Image imgItemIcon;

	[SerializeField]
	private TMP_Text txtItemCount;

	[SerializeField]
	private Text txtItemInfo;

	public int count
	{
		set
		{
			txtItemCount.text = value.ToString();
		}
	}

	public void Render(ItemQuantityData data)
	{
		SetSprite(imgItemIcon, data.icon);
		txtItemCount.text = data.currentCount.ToString();
		txtItemInfo.text = data.info;
	}
}
