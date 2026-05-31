using UnityEngine;

public static class DolocColor
{
	public static Color empty = new Color(1f, 1f, 1f, 0f);

	public static Color white = new Color(1f, 1f, 1f);

	public static Color red = Color.red;

	public static Color drakRed = new Color(0.6196079f, 0.2039216f, 0.3372549f);

	public static Color lightGreen = new Color(69f / 85f, 1f, 0.4392157f);

	public static Color green = new Color(0.23529412f, 0.6392157f, 0.4392157f);

	public static Color pink = new Color(1f, 0.41960785f, 0.5921569f);

	public static Color blue = new Color(0.3019608f, 0.6509804f, 1f);

	public static Color gray = new Color(0.5f, 0.5f, 0.5f);

	public static Color purple = new Color(0.2784314f, 0.23137255f, 0.47058824f);

	public static Color orange = new Color(1f, 29f / 51f, 0.4f);

	public static Color yellow = new Color(1f, 76f / 85f, 0.47058824f);

	public static Color darkgrey = new Color(0.176f, 0.176f, 0.176f, 1f);

	public static Color lightgrey = new Color(0.85f, 0.85f, 0.85f, 1f);

	public static Color buttonNormal = new Color(1f, 0.992f, 0.89f);

	public static Color buttonHover = new Color(0.356f, 0.415f, 0.45f);

	public static Color buttonDown = new Color(0.184f, 0.203f, 0.25f);

	public static Color slienceUiColor_Purple = new Color(0.3372549f, 0.3333333f, 0.6509804f);

	public static Color eyecatchUiColor_Cyan = new Color(0f, 1f, 0.64705f);

	public static Color healthColor = new Color(0f, 0f, 0f);

	public static string ToHex(this Color color)
	{
		return "#" + ColorUtility.ToHtmlStringRGB(color);
	}
}
