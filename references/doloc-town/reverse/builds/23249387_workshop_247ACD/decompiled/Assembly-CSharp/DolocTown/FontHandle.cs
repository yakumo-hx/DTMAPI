using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown;

[DisallowMultipleComponent]
public class FontHandle : DolocObject
{
	private string _handlePath;

	public FontInfo CurrentFontInfo { get; private set; }

	public string HandlePath
	{
		get
		{
			if (_handlePath == null)
			{
				_handlePath = FontManagerUtils.BuildPath(base.gameObject);
			}
			return _handlePath;
		}
	}

	private void OutputOriginInfo()
	{
		if (!DolocAPI.FontManager.TryGetFontHandleInfo(this, out var info))
		{
			Debug.LogError("该对象还未切换字体");
		}
		else
		{
			Debug.Log(info);
		}
	}

	private void OnEnable()
	{
		DolocAPI.FontManager.ValidateFontHandle(this);
	}

	public void SetFont(FontInfo font, FontHandleInfo handleInfo)
	{
		if (font.LocalizationInfo != null)
		{
			CurrentFontInfo = font;
			if (font.isPixelFont)
			{
				_SetPixelFont(font, handleInfo);
			}
			else
			{
				_SetVectorFont(font, handleInfo);
			}
		}
	}

	public FontHandleInfo GetHandleInfo()
	{
		Font font;
		TMP_FontAsset tmpFontAsset;
		int fontSize;
		TextType textType = GetTextType(out font, out tmpFontAsset, out fontSize);
		return new FontHandleInfo(HandlePath, textType, font, tmpFontAsset, isPixelFont: true, fontSize);
	}

	public void ResetFont(FontHandleInfo info)
	{
		CurrentFontInfo = new FontInfo("zh-CN", isPixelFont: true);
		switch (info.textType)
		{
		case TextType.None:
			break;
		case TextType.Legacy:
		{
			Text component2 = GetComponent<Text>();
			if (component2 != null)
			{
				component2.font = info.font;
			}
			break;
		}
		case TextType.TMP:
		{
			TMP_Text component = GetComponent<TMP_Text>();
			if (component != null)
			{
				component.font = info.tmpFontAsset;
			}
			break;
		}
		}
	}

	private void _SetVectorFont(FontInfo font, FontHandleInfo originInfo)
	{
		Text component = GetComponent<Text>();
		if (component != null)
		{
			if (font.LocalizationInfo.Font.Asset != null)
			{
				component.font = font.LocalizationInfo.Font.Asset;
			}
			else
			{
				component.font = originInfo.font;
			}
			return;
		}
		TMP_Text component2 = GetComponent<TMP_Text>();
		if (!(component2 == null))
		{
			if (font.LocalizationInfo.TmpFont.Asset != null)
			{
				component2.font = font.LocalizationInfo.TmpFont.Asset;
			}
			else
			{
				component2.font = originInfo.tmpFontAsset;
			}
		}
	}

	private void _SetPixelFont(FontInfo font, FontHandleInfo originInfo)
	{
		Text component = GetComponent<Text>();
		if (component != null)
		{
			Font font2 = font.ChoosePixelFont(originInfo.pxFontSize);
			if (font2 != null)
			{
				component.font = font2;
			}
			else
			{
				component.font = originInfo.font;
			}
			return;
		}
		TMP_Text component2 = GetComponent<TMP_Text>();
		if (!(component2 == null))
		{
			TMP_FontAsset tMP_FontAsset = font.ChoosePixelTmpFont(originInfo.pxFontSize);
			if (tMP_FontAsset != null)
			{
				component2.font = tMP_FontAsset;
			}
			else
			{
				component2.font = originInfo.tmpFontAsset;
			}
		}
	}

	private TextType GetTextType(out Font font, out TMP_FontAsset tmpFontAsset, out int fontSize)
	{
		Text component = GetComponent<Text>();
		if (component != null)
		{
			font = component.font;
			tmpFontAsset = null;
			fontSize = component.fontSize;
			return TextType.Legacy;
		}
		fontSize = 10;
		TMP_Text component2 = GetComponent<TMP_Text>();
		if (component2 != null)
		{
			font = null;
			tmpFontAsset = component2.font;
			fontSize = (int)component2.fontSize;
			return TextType.TMP;
		}
		font = null;
		tmpFontAsset = null;
		return TextType.None;
	}
}
