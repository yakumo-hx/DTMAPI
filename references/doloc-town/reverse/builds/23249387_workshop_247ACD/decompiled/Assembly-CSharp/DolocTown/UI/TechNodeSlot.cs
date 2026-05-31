using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TechNodeSlot : DolocNavigationButton
{
	[SerializeField]
	private Color bgColorLv1;

	[SerializeField]
	private Color bgColorLv2;

	[SerializeField]
	private Color bgColorLv3;

	[SerializeField]
	private Transform imgLock;

	[SerializeField]
	private Text title;

	public Vector2Int nodePos { get; private set; }

	public void Render(TechNodeData data)
	{
		nodePos = data.nodePos;
		base.iconSprite = data.icon;
		title.text = data.title;
		imgLock.gameObject.SetActive(value: false);
		switch (data.nodeStatus)
		{
		case TechNodeStatus.Unlocked:
			base.backgroundColor = bgColorLv1;
			iconImg.material = null;
			break;
		case TechNodeStatus.PreUnlockedAndAvailable:
			base.backgroundColor = bgColorLv2;
			iconImg.material = LocMaterials.UI_MAT_GREY;
			break;
		case TechNodeStatus.PreUnlockedButUnavailable:
		case TechNodeStatus.PreLocked:
			base.backgroundColor = bgColorLv3;
			iconImg.material = LocMaterials.UI_MAT_GREY;
			break;
		case TechNodeStatus.VersionUnavailable:
			base.backgroundColor = bgColorLv3;
			iconImg.material = LocMaterials.UI_MAT_GREY;
			imgLock.gameObject.SetActive(value: true);
			break;
		}
	}
}
