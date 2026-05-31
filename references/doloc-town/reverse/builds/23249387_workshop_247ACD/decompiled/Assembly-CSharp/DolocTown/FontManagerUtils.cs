using UnityEngine;

namespace DolocTown;

public static class FontManagerUtils
{
	public static bool IsSameLocalizationFont(this FontInfo L, FontInfo R)
	{
		if (L.LocalizationInfo == null || R.LocalizationInfo == null)
		{
			return false;
		}
		if (L.l10nId == R.l10nId)
		{
			return L.isPixelFont == R.isPixelFont;
		}
		return false;
	}

	private static string BuildPath(Transform trans)
	{
		if (trans.transform.parent == null)
		{
			return trans.name;
		}
		return BuildPath(trans.parent) + "/" + trans.name;
	}

	public static string BuildPath(GameObject obj)
	{
		if (obj == null)
		{
			return null;
		}
		return BuildPath(obj.transform);
	}
}
