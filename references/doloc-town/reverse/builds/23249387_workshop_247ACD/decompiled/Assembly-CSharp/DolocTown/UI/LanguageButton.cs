using DolocTown.Config;
using DolocTown.Config.Settings;
using UnityEngine;

namespace DolocTown.UI;

public class LanguageButton : DolocNavigationButton
{
	[SerializeField]
	public string l10nId;

	protected override void __Init()
	{
		base.__Init();
		onClick.AddListener(delegate
		{
			SwitchLanguage();
		});
	}

	public void SwitchLanguage()
	{
		if (DolocConfig.Tables.TbLocalization.GetOrDefault(l10nId ?? "") != null)
		{
			DolocAPI.userSettings.SetValue(UserSettingType.LANGUAGE_TEXT, l10nId);
			DolocAPI.SaveUserSettings();
			DolocAPI.gameUiStates.GetState<HomePageUiState>().Refresh();
		}
	}
}
