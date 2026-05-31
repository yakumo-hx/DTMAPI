using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Settings;
using UnityEngine;

namespace DolocTown;

public class FontManagerUsage : MonoBehaviour
{
	[SerializeField]
	private string language;

	[SerializeField]
	private bool isPixelFont;

	private static IEnumerable<string> AvailableLanguages => DolocConfig.Tables.TbLocalization.DataMap.Keys.ToArray();

	private void OnLanguageChanged(string lang)
	{
		DolocAPI.SwitchLanguage(lang);
	}

	private void OnPixelFontChanged(bool isPixel)
	{
		DolocAPI.userSettings.SetValue(UserSettingType.TEXT_USE_PIXEL_FONT, isPixel);
		DolocAPI.SaveUserSettings();
	}

	private void ReplaceFont()
	{
		if (DolocConfig.Tables.TbLocalization.DataMap.TryGetValue(language, out var _))
		{
			DolocAPI.FontManager.SetFont(new FontInfo(language, isPixelFont));
		}
	}

	private void RestoreFont()
	{
		DolocAPI.FontManager.SetFont(new FontInfo("zh-CN", isPixelFont: true));
	}
}
