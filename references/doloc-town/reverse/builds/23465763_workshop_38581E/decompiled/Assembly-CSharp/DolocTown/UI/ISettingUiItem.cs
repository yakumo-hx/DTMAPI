using DolocTown.Config.Settings;
using UnityEngine.UI;

namespace DolocTown.UI;

public interface ISettingUiItem
{
	bool activeSelf { get; }

	string title { get; set; }

	UserSettingInfo config { get; set; }

	UserSettingType settingId => config.Id;

	Selectable selectable { get; }

	bool shouldShowArrow { get; }

	float height { get; }
}
