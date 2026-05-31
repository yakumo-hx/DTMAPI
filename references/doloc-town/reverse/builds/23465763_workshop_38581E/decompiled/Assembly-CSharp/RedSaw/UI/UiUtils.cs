using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.UI;

public static class UiUtils
{
	public static Image createImage(Transform container)
	{
		GameObject gameObject = new GameObject("RedSaw_Image");
		gameObject.transform.SetParent(container);
		Image result = gameObject.AddComponent<Image>();
		RectTransform component = gameObject.GetComponent<RectTransform>();
		component.anchorMax = Vector2.zero;
		component.anchorMin = Vector2.zero;
		component.pivot = Vector2.zero;
		return result;
	}

	public static Sprite generateSinglePixel(int pixelPerUnit = 8)
	{
		Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, 1, linear: true);
		texture2D.filterMode = FilterMode.Point;
		texture2D.SetPixel(0, 0, Color.white);
		texture2D.Apply();
		return Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), Vector2.zero, pixelPerUnit);
	}

	private static void fillTexture(Texture2D texture, RectInt rect, Color c)
	{
		int num = rect.width + rect.x;
		int num2 = rect.height + rect.y;
		for (int i = rect.x; i < num; i++)
		{
			for (int j = rect.y; j < num2; j++)
			{
				texture.SetPixel(i, j, c);
			}
		}
	}

	public static Sprite[] getPanelBackground(int pixelPerUnit = 8)
	{
		Sprite[] array = new Sprite[4];
		Color color = new Color(0f, 0f, 0f, 0f);
		int width = 4;
		int height = 4;
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, 1, linear: true)
		{
			filterMode = FilterMode.Point
		};
		fillTexture(texture2D, new RectInt(0, 0, width, height), Color.white);
		texture2D.SetPixel(0, 3, color);
		texture2D.SetPixel(0, 2, color);
		texture2D.SetPixel(0, 1, color);
		texture2D.SetPixel(1, 3, color);
		texture2D.SetPixel(2, 3, color);
		texture2D.Apply();
		array[0] = Sprite.Create(texture2D, new Rect(0f, 0f, 4f, 4f), Vector2.one, pixelPerUnit);
		Texture2D texture2D2 = new Texture2D(width, height, TextureFormat.RGBA32, 1, linear: true)
		{
			filterMode = FilterMode.Point
		};
		fillTexture(texture2D2, new RectInt(0, 0, width, height), Color.white);
		texture2D2.SetPixel(0, 0, color);
		texture2D2.SetPixel(0, 1, color);
		texture2D2.SetPixel(0, 2, color);
		texture2D2.SetPixel(1, 0, color);
		texture2D2.SetPixel(2, 0, color);
		texture2D2.Apply();
		array[1] = Sprite.Create(texture2D2, new Rect(0f, 0f, 4f, 4f), Vector2.one, pixelPerUnit);
		Texture2D texture2D3 = new Texture2D(width, height, TextureFormat.RGBA32, 1, linear: true)
		{
			filterMode = FilterMode.Point
		};
		fillTexture(texture2D3, new RectInt(0, 0, width, height), Color.white);
		texture2D3.SetPixel(1, 3, color);
		texture2D3.SetPixel(2, 3, color);
		texture2D3.SetPixel(3, 3, color);
		texture2D3.SetPixel(3, 2, color);
		texture2D3.SetPixel(3, 1, color);
		texture2D3.Apply();
		array[2] = Sprite.Create(texture2D3, new Rect(0f, 0f, 4f, 4f), Vector2.one, pixelPerUnit);
		Texture2D texture2D4 = new Texture2D(width, height, TextureFormat.RGBA32, 1, linear: true)
		{
			filterMode = FilterMode.Point
		};
		fillTexture(texture2D4, new RectInt(0, 0, width, height), Color.white);
		texture2D4.SetPixel(1, 0, color);
		texture2D4.SetPixel(2, 0, color);
		texture2D4.SetPixel(3, 0, color);
		texture2D4.SetPixel(3, 1, color);
		texture2D4.SetPixel(3, 2, color);
		texture2D4.Apply();
		array[3] = Sprite.Create(texture2D4, new Rect(0f, 0f, 4f, 4f), Vector2.one, pixelPerUnit);
		return array;
	}

	public static Sprite[] getDefaultTiledBackground(int pixelPerUnit = 8)
	{
		Sprite sprite = generateSinglePixel(pixelPerUnit);
		Color color = new Color(0f, 0f, 0f, 0f);
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, 1, linear: true);
		texture2D.filterMode = FilterMode.Point;
		texture2D.SetPixel(0, 1, color);
		texture2D.SetPixel(1, 1, Color.white);
		texture2D.SetPixel(1, 0, Color.white);
		texture2D.SetPixel(0, 0, Color.white);
		texture2D.Apply();
		Sprite sprite2 = Sprite.Create(texture2D, new Rect(0f, 0f, 2f, 2f), Vector2.one, pixelPerUnit);
		Texture2D texture2D2 = new Texture2D(2, 2, TextureFormat.RGBA32, 1, linear: true);
		texture2D2.filterMode = FilterMode.Point;
		texture2D2.SetPixel(0, 1, Color.white);
		texture2D2.SetPixel(1, 1, color);
		texture2D2.SetPixel(1, 0, Color.white);
		texture2D2.SetPixel(0, 0, Color.white);
		texture2D2.Apply();
		Sprite sprite3 = Sprite.Create(texture2D2, new Rect(0f, 0f, 2f, 2f), Vector2.one, pixelPerUnit);
		Texture2D texture2D3 = new Texture2D(2, 2, TextureFormat.RGBA32, 1, linear: true);
		texture2D3.filterMode = FilterMode.Point;
		texture2D3.SetPixel(0, 1, Color.white);
		texture2D3.SetPixel(1, 1, Color.white);
		texture2D3.SetPixel(1, 0, color);
		texture2D3.SetPixel(0, 0, Color.white);
		texture2D3.Apply();
		Sprite sprite4 = Sprite.Create(texture2D3, new Rect(0f, 0f, 2f, 2f), Vector2.one, pixelPerUnit);
		Texture2D texture2D4 = new Texture2D(2, 2, TextureFormat.RGBA32, 1, linear: true);
		texture2D4.filterMode = FilterMode.Point;
		texture2D4.SetPixel(0, 1, Color.white);
		texture2D4.SetPixel(1, 1, Color.white);
		texture2D4.SetPixel(1, 0, Color.white);
		texture2D4.SetPixel(0, 0, color);
		texture2D4.Apply();
		Sprite sprite5 = Sprite.Create(texture2D4, new Rect(0f, 0f, 2f, 2f), Vector2.one, pixelPerUnit);
		return new Sprite[5] { sprite2, sprite5, sprite3, sprite4, sprite };
	}
}
