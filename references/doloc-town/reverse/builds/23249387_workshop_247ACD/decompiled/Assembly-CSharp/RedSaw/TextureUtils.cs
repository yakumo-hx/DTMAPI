using UnityEngine;
using UnityEngine.Sprites;

namespace RedSaw;

public static class TextureUtils
{
	public static Texture2D CreatePureColorTexture(Color color, int width = 1, int height = 1)
	{
		Texture2D texture2D = new Texture2D(width, height);
		Color[] array = new Color[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = color;
		}
		texture2D.SetPixels(array);
		texture2D.Apply();
		return texture2D;
	}

	public static Color[] GetSpritePixels(this Sprite sprite)
	{
		if (sprite == null)
		{
			return null;
		}
		Texture2D texture = sprite.texture;
		Vector4 outerUV = DataUtility.GetOuterUV(sprite);
		Rect rect = sprite.rect;
		int x = (int)(outerUV.x * (float)texture.width);
		int y = (int)(outerUV.y * (float)texture.height);
		int blockWidth = (int)rect.width;
		int blockHeight = (int)rect.height;
		return texture.GetPixels(x, y, blockWidth, blockHeight);
	}
}
