using DolocTown.Config.Settings;
using TMPro;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class SettingItemUI : DolocUiObject, ISettingUiItem
{
	private TextMeshProUGUI txtTitle;

	public bool activeSelf => base.gameObject.activeSelf;

	public string title
	{
		get
		{
			return txtTitle.text;
		}
		set
		{
			txtTitle.text = value;
		}
	}

	public UserSettingInfo config { get; set; }

	public UserSettingType settingId => config.Id;

	public virtual Selectable selectable => null;

	public virtual bool shouldShowArrow => true;

	protected override void __Init()
	{
		base.__Init();
		txtTitle = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
	}
}
