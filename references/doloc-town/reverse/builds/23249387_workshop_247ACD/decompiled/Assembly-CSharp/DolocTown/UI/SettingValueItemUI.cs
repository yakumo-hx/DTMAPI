using DolocTown.Config.Settings;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class SettingValueItemUI<T> : DolocUiObject, ISettingUiItemWithValue<T>, ISettingUiItem
{
	protected TextMeshProUGUI txtTitle;

	private DolocButtonComponent backgroundButton;

	public bool activeSelf => base.gameObject.activeSelf;

	public virtual string title
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

	public abstract T currentValue { get; }

	public UserSettingInfo config { get; set; }

	public UserSettingType settingId => config.Id;

	public virtual UnityEvent<T> onValueChanged { get; private set; }

	public abstract Selectable selectable { get; }

	public virtual bool shouldShowArrow => true;

	protected override void __Init()
	{
		base.__Init();
		txtTitle = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
		backgroundButton = GetComponent<DolocButtonComponent>();
		backgroundButton.onClick.AddListener(selectable.Select);
		onValueChanged = new UnityEvent<T>();
		onValueChanged.AddListener(delegate(T value)
		{
			DolocAPI.userSettings.SetValue(settingId, value);
		});
	}
}
