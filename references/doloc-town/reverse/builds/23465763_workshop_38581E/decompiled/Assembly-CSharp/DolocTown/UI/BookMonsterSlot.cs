using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BookMonsterSlot : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private GameObject redPoint;

	public void Render(BookMonsterData data)
	{
		base.iconSprite = data.icon;
		title.text = data.name;
		base.iconColor = (data.isVisited ? Color.white : Color.black);
		redPoint.SetActive(data.isNew);
	}

	protected override void OnSelect()
	{
		base.OnSelect();
		DolocAPI.HideHoverBox();
	}
}
