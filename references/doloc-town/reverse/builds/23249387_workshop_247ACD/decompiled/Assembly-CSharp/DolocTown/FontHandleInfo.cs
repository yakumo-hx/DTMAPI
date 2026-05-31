using TMPro;
using UnityEngine;

namespace DolocTown;

public readonly struct FontHandleInfo
{
	private const int PX12 = 12;

	private const int PX10 = 10;

	public readonly string objPath;

	public readonly TextType textType;

	public readonly Font font;

	public readonly TMP_FontAsset tmpFontAsset;

	public readonly bool isPixelFont;

	public readonly int fontSize;

	public readonly int pxFontSize;

	public FontHandleInfo(string objPath, TextType textType, Font font, TMP_FontAsset tmpFontAsset, bool isPixelFont, int fontSize)
	{
		this.objPath = objPath;
		this.textType = textType;
		this.font = font;
		this.tmpFontAsset = tmpFontAsset;
		this.isPixelFont = isPixelFont;
		this.fontSize = fontSize;
		pxFontSize = ((fontSize % 12 == 0) ? 12 : 10);
	}

	public override string ToString()
	{
		return $"FontHandleInfo: objPath={objPath}, textType={textType}, font={font}, tmpFontAsset={tmpFontAsset}, isPixelFont={isPixelFont}, fontSize={fontSize}";
	}
}
