using DolocTown.Config;
using DolocTown.Config.Localization;
using TMPro;
using UnityEngine;

namespace DolocTown;

public struct FontInfo
{
	public readonly string l10nId;

	public readonly bool isPixelFont;

	public LocalizationInfo LocalizationInfo => DolocConfig.Tables.TbLocalization.GetOrDefault(l10nId ?? string.Empty);

	public FontInfo(string l10nId, bool isPixelFont)
	{
		this.l10nId = l10nId;
		this.isPixelFont = isPixelFont;
	}

	public Font ChoosePixelFont(int fontPx)
	{
		if (fontPx == 12)
		{
			return LocalizationInfo.Font12px.Asset;
		}
		return LocalizationInfo.Font10px.Asset;
	}

	public TMP_FontAsset ChoosePixelTmpFont(int fontPx)
	{
		if (fontPx == 12)
		{
			return LocalizationInfo.TmpFont12px.Asset;
		}
		return LocalizationInfo.TmpFont10px.Asset;
	}
}
