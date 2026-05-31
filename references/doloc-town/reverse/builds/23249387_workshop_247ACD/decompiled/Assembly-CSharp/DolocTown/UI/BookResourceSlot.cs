using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BookResourceSlot : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private GameObject redPoint;

	public void Render(BookResourceData data)
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
