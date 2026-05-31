using UnityEngine;

namespace DolocTown.UI;

public class TopLabelTitleMenu : TitleMenu
{
	protected override GameObject slotPrefab => DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_PFB_TEXT_LABEL_STYLE);
}
