using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DateEventInfo : DolocUiRecyclableObject
{
	[SerializeField]
	private Text title;

	[SerializeField]
	private Text description;

	public void Render(string title, string desc)
	{
		this.title.text = title;
		description.gameObject.SetActive(!string.IsNullOrEmpty(desc));
		description.text = desc;
	}
}
