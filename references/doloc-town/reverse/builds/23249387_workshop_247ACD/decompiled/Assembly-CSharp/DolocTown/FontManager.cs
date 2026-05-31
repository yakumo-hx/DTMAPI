using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Settings;
using UnityEngine;

namespace DolocTown;

public class FontManager
{
	private readonly Dictionary<string, FontHandleInfo> FontHandleInfos = new Dictionary<string, FontHandleInfo>();

	private FontInfo _currentFontInfo;

	private static IEnumerable<FontHandle> FontHandles => Object.FindObjectsOfType<FontHandle>();

	public bool TryGetFontHandleInfo(FontHandle handle, out FontHandleInfo info)
	{
		info = default(FontHandleInfo);
		if (handle == null)
		{
			return false;
		}
		return FontHandleInfos.TryGetValue(handle.HandlePath, out info);
	}

	public void AdaptToLanguage(string l10nId)
	{
		if (!l10nId.IsNullOrEmpty())
		{
			if (!DolocConfig.Tables.TbLocalization.DataMap.TryGetValue(l10nId, out var _))
			{
				Debug.LogError("没有找到本地化语言配置:\"" + l10nId + "\"");
				return;
			}
			bool orDefault = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.TEXT_USE_PIXEL_FONT);
			SetFont(new FontInfo(l10nId, orDefault));
		}
	}

	public void ValidateFontHandle(FontHandle handle)
	{
		if (!_currentFontInfo.IsSameLocalizationFont(handle.CurrentFontInfo))
		{
			if (!FontHandleInfos.TryGetValue(handle.HandlePath, out var value))
			{
				value = handle.GetHandleInfo();
				FontHandleInfos.Add(handle.HandlePath, value);
			}
			handle.SetFont(_currentFontInfo, value);
		}
	}

	public void SetFont(FontInfo fontInfo)
	{
		if (_currentFontInfo.IsSameLocalizationFont(fontInfo))
		{
			Debug.Log("当前字体与目标字体相同，无须切换");
			return;
		}
		_currentFontInfo = fontInfo;
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		foreach (FontHandle fontHandle in FontHandles)
		{
			if (!hashSet.Add(fontHandle.gameObject))
			{
				Debug.LogWarning("检测到重复的FontHandle对象，路径为：" + fontHandle.HandlePath + "，请检查是否有重复的FontHandle组件！");
			}
			if (!FontHandleInfos.TryGetValue(fontHandle.HandlePath, out var value))
			{
				value = fontHandle.GetHandleInfo();
				FontHandleInfos.Add(fontHandle.HandlePath, value);
			}
			fontHandle.SetFont(fontInfo, value);
		}
		hashSet.Clear();
		Debug.Log($"字体已切换为{fontInfo.l10nId} {fontInfo.isPixelFont}");
	}

	private void ResetToDefaultFont()
	{
		foreach (FontHandle fontHandle in FontHandles)
		{
			if (FontHandleInfos.TryGetValue(fontHandle.HandlePath, out var value))
			{
				fontHandle.ResetFont(value);
			}
		}
	}

	public void RegisterUserSettingsListener()
	{
		DolocAPI.RegisterMsgListener(UserSettingType.TEXT_USE_PIXEL_FONT, OnPixelFontChanged);
	}

	private void OnPixelFontChanged(object sender, GameEventArgs args)
	{
		string orDefault = DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.LANGUAGE_TEXT);
		bool orDefault2 = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.TEXT_USE_PIXEL_FONT);
		if (!DolocConfig.Tables.TbLocalization.DataMap.TryGetValue(orDefault, out var _))
		{
			Debug.LogError("没有找到本地化语言配置:\"" + orDefault + "\"");
		}
		else
		{
			SetFont(new FontInfo(orDefault, orDefault2));
		}
	}
}
