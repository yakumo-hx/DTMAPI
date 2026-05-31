using UnityEngine.UI;

namespace DolocTown.UI;

public class WeatherSlot : DolocNavigationButton
{
	private Text timeInfo;

	protected override void __Init()
	{
		base.__Init();
		timeInfo = GetComponentInChildren<Text>();
	}

	public void SetTimeInfoVisible(bool value)
	{
		timeInfo.gameObject.SetActive(value);
	}

	protected override void OnHighLighted(bool value)
	{
		base.OnHighLighted(value);
		timeInfo.color = (value ? DolocUiColor.EYECATCHCOLOR_CYAN : DolocUiColor.SLIENTCOLOR_GREY);
	}
}
