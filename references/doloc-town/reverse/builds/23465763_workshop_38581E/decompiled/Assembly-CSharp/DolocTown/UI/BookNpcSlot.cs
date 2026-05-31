using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BookNpcSlot : DolocNavigationButton
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private GameObject redPoint;

	public void Render(BookNpcData data)
	{
		base.iconSprite = data.icon;
		title.text = data.name;
		base.iconColor = (data.isVisited ? Color.white : Color.black);
		redPoint.SetActive(data.isNew);
	}
}
