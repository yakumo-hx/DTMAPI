using UnityEngine;

namespace DolocTown.UI;

public class HomePageTextMenu : TextMenu
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_HOMEPAGE_BUTTON);
}
