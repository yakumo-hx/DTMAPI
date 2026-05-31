using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FactionItemSlot : DolocNavigationButton
{
	[SerializeField]
	private TMP_Text txtCount;

	[SerializeField]
	private Text txtHint;

	public void Render(FactionItemData data)
	{
		base.iconSprite = data.sprite;
		base.iconColor = ((base.iconSprite == null) ? DolocColor.empty : Color.white);
		base.grayed = !data.isFinished;
		txtCount.gameObject.SetActive(!data.isFinished);
		txtHint.gameObject.SetActive(data.isFinished);
		txtCount.text = data.count;
	}

	protected override void OnGrayed(bool value)
	{
		buttonCanvasGroup.alpha = (value ? 0.3f : 1f);
	}
}
