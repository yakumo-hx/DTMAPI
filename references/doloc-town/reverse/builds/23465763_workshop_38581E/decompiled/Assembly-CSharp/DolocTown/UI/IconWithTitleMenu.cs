using UnityEngine;

namespace DolocTown.UI;

public class IconWithTitleMenu : TitleMenu
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_PFB_ICON_WITH_TEXT_STYLE);

	public void Render(Sprite[] icons, string[] labels)
	{
		Render(labels);
		for (int i = 0; i < base.slots.Count; i++)
		{
			base.slots[i].iconSprite = icons[i];
		}
	}
}
