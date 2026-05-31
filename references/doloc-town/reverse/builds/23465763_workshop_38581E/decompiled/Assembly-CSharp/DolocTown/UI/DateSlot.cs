using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DateSlot : DolocNavigationButton
{
	[SerializeField]
	private Text time;

	[SerializeField]
	private GameObject memoMarking;

	[SerializeField]
	private Image festival;

	[SerializeField]
	private GameObject playerBirthday;

	[SerializeField]
	private Color highLightBgColor;

	public void Render(DateData data)
	{
		base.highLighted = data.isSameDay;
		iconImg.gameObject.SetActive(data.birthdayIcon != null);
		festival.gameObject.SetActive(data.festivalIcon != null);
		memoMarking.SetActive(data.hasMemo);
		playerBirthday.SetActive(data.isPlayerBirthday);
		time.text = data.time.ToString();
		iconImg.sprite = data.birthdayIcon;
		festival.sprite = data.festivalIcon;
	}

	protected override void OnHighLighted(bool value)
	{
		base.backgroundColor = (value ? highLightBgColor : DolocColor.empty);
	}
}
