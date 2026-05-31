using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BookCreatureSlot : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private GameObject redPoint;

	public void Render(BookCreatureData data)
	{
		SetSprite(iconImg, data.icon, autoSize: true);
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
