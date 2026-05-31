using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EquipmentSlot : CraftSlot<EquipmentData>
{
	[SerializeField]
	private Text desc1;

	[SerializeField]
	private Text desc2;

	[SerializeField]
	private Button collect;

	[SerializeField]
	private GameObject makeIcon;

	[SerializeField]
	private GameObject mask;

	[SerializeField]
	public Color normalColor;

	[SerializeField]
	public Color selectedColor;

	[SerializeField]
	public Color highlightColor;

	public Button BtnCollect => collect;

	public bool IsUnLock { get; private set; }

	public override void Render(EquipmentData data)
	{
		base.iconSprite = data.outputItemSprite;
		title.text = data.recipeTitle;
		desc1.text = data.subType;
		desc2.text = data.sizeDescription;
		collect.image.sprite = data.smallCollectIcon;
		base.iconColor = (data.showCompleteInfo ? Color.white : Color.black);
		collect.gameObject.SetActive(data.showCompleteInfo);
		makeIcon.gameObject.SetActive(data.isUnlock && data.isCostEnough);
		IsUnLock = data.isUnlock;
		mask.SetActive(!data.isUnlock);
	}
}
