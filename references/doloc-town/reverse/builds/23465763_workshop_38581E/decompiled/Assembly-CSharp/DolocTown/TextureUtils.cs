using System.IO;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public static class TextureUtils
{
	public static Texture2D CreateTexture(Vector2Int size)
	{
		return CreateTexture(size.x, size.y, DolocColor.empty);
	}

	public static Texture2D CreateTexture(int width, int height)
	{
		return CreateTexture(width, height, DolocColor.empty);
	}

	public static Texture2D CreateTexture(Vector2Int size, Color color)
	{
		return CreateTexture(size.x, size.y, color);
	}

	public static Texture2D CreateTexture(int width, int height, Color color)
	{
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
		texture2D.FillColor(color);
		texture2D.filterMode = FilterMode.Point;
		return texture2D;
	}

	public static void Clear(this Texture2D texture)
	{
		texture.FillColor(DolocColor.empty);
	}

	public static void FillColor(this Texture2D texture, Color color, bool apply = true)
	{
		texture.DrawArea(0, 0, texture.width - 1, texture.height - 1, color, apply);
	}

	public static void SaveAsPng(this Texture2D texture, string folderPath, string fileName)
	{
		texture.SaveAsPng(Path.Join(folderPath, fileName));
	}

	public static void SaveAsPng(this Texture2D texture, string outputPath)
	{
		if (!outputPath.EndsWith(".png"))
		{
			outputPath += ".png";
		}
		string directoryName = Path.GetDirectoryName(outputPath);
		if (!Directory.Exists(directoryName) && directoryName != null)
		{
			Directory.CreateDirectory(directoryName);
		}
		texture.Apply();
		byte[] bytes = texture.EncodeToPNG();
		File.WriteAllBytes(outputPath, bytes);
		Debug.Log("PNG 图像已保存到: " + outputPath);
	}

	public static Texture2D Trim(this Texture2D texture)
	{
		Rect trimmedRect;
		return texture.Trim(out trimmedRect);
	}

	public static Texture2D Trim(this Texture2D texture, out Rect trimmedRect)
	{
		trimmedRect = GetNonEmptyRect(texture);
		return texture.CropTexture(trimmedRect);
	}

	public static Texture2D Copy(this Texture2D texture)
	{
		Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, mipChain: false);
		texture2D.SetPixels(texture.GetPixels());
		texture2D.Apply();
		return texture2D;
	}

	private static Rect GetNonEmptyRect(Texture2D texture)
	{
		int width = texture.width;
		int height = texture.height;
		int num = width;
		int num2 = height;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				if (texture.GetPixel(j, i).a > 0.01f)
				{
					if (j < num)
					{
						num = j;
					}
					if (i < num2)
					{
						num2 = i;
					}
					if (j > num3)
					{
						num3 = j;
					}
					if (i > num4)
					{
						num4 = i;
					}
				}
			}
		}
		int num5 = num3 - num + 1;
		int num6 = num4 - num2 + 1;
		return new Rect(num, num2, num5, num6);
	}

	public static Texture2D CropTexture(this Texture2D texture, Rect rect)
	{
		int num = (int)rect.width;
		int num2 = (int)rect.height;
		Texture2D texture2D = new Texture2D(num, num2);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Color pixel = texture.GetPixel((int)rect.x + j, (int)rect.y + i);
				texture2D.SetPixel(j, i, pixel);
			}
		}
		texture2D.Apply();
		return texture2D;
	}

	public static void DrawArea(this Texture2D texture, Vector2Int offset, Vector2Int size, Color color, bool apply = true)
	{
		int num = Mathf.Clamp(offset.x, 0, texture.width);
		int num2 = Mathf.Clamp(offset.y, 0, texture.height);
		int num3 = Mathf.Clamp(offset.x + size.x, 0, texture.width);
		int num4 = Mathf.Clamp(offset.y + size.y, 0, texture.height);
		int num5 = num3 - num;
		int num6 = num4 - num2;
		if (num5 > 0 && num6 > 0)
		{
			Color[] array = new Color[num5 * num6];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = color;
			}
			texture.SetPixels(num, num2, num5, num6, array);
			if (apply)
			{
				texture.Apply();
			}
		}
	}

	public static void DrawArea(this Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color, bool apply = true)
	{
		texture.DrawArea(new Vector2Int(xMin, yMin), new Vector2Int(xMax - xMin + 1, yMax - yMin + 1), color, apply);
	}

	public static void DrawAreaByTexture(this Texture2D texture, Vector2Int offset, Vector2Int size, Texture2D otherTexture, bool apply = true)
	{
		int blockWidth = Mathf.Min(size.x, otherTexture.width);
		int blockHeight = Mathf.Min(size.y, otherTexture.height);
		Color[] pixels = otherTexture.GetPixels(0, 0, blockWidth, blockHeight);
		texture.SetPixels(offset.x, offset.y, blockWidth, blockHeight, pixels);
		if (apply)
		{
			texture.Apply();
		}
	}

	public static void DrawRectangle(this Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color, bool apply = true)
	{
		texture.DrawLine(xMin, yMax, xMax, yMax, color, apply: false);
		texture.DrawLine(xMin, yMin, xMax, yMin, color, apply: false);
		texture.DrawLine(xMin, yMin, xMin, yMax, color, apply: false);
		texture.DrawLine(xMax, yMin, xMax, yMax, color, apply: false);
		if (apply)
		{
			texture.Apply();
		}
	}

	public static void DrawLine(this Texture2D texture, int x1, int y1, int x2, int y2, Color color, bool apply = true)
	{
		int num = Mathf.Abs(x2 - x1);
		int num2 = Mathf.Abs(y2 - y1);
		int num3 = ((x1 < x2) ? 1 : (-1));
		int num4 = ((y1 < y2) ? 1 : (-1));
		int num5 = num - num2;
		while (true)
		{
			texture.SetPixel(x1, y1, color);
			if (x1 == x2 && y1 == y2)
			{
				break;
			}
			int num6 = 2 * num5;
			if (num6 > -num2)
			{
				num5 -= num2;
				x1 += num3;
			}
			if (num6 < num)
			{
				num5 += num;
				y1 += num4;
			}
		}
		if (apply)
		{
			texture.Apply();
		}
	}

	public static Texture2D PaddingTexture(this Texture2D texture, Vector2Int size, Vector2Int offset)
	{
		int x = size.x;
		int y = size.y;
		Texture2D texture2D = CreateTexture(x, y);
		int num = Mathf.Min(texture.width, x);
		int num2 = Mathf.Min(texture.height, y);
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Color pixel = texture.GetPixel(j, i);
				texture2D.SetPixel(offset.x + j, offset.y + i, pixel);
			}
		}
		return texture2D;
	}

	public static Texture2D CropAndCenterTexture(this Texture2D texture, Vector2Int size, Vector2Int additionOffset, out Vector2Int paddingOffset)
	{
		Texture2D texture2D = texture.Trim();
		int x = Mathf.Max(0, (size.x - texture2D.width) / 2) + additionOffset.x;
		int y = Mathf.Max(0, (size.y - texture2D.height) / 2) + additionOffset.y;
		paddingOffset = new Vector2Int(x, y);
		return texture.PaddingTexture(size, paddingOffset);
	}

	public static Sprite PaddingSprite(Sprite sprite, int padding)
	{
		Vector2 size = sprite.rect.size;
		Vector2Int pxSize = new Vector2Int((int)size.x, (int)size.y);
		Color[] spritePixels = sprite.GetSpritePixels();
		int num = padding * 2;
		Vector2Int bufferSize = new Vector2Int(pxSize.x + num, pxSize.y + num);
		Color[] array = new Color[bufferSize.x * bufferSize.y];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Color.clear;
		}
		WriteColorsIntoBuffer(array, bufferSize, spritePixels, pxSize, Vector2Int.one);
		Texture2D texture2D = new Texture2D(bufferSize.x, bufferSize.y);
		texture2D.filterMode = FilterMode.Point;
		texture2D.SetPixels(0, 0, bufferSize.x, bufferSize.y, array);
		texture2D.Apply();
		return Sprite.Create(texture2D, new Rect(0f, 0f, bufferSize.x, bufferSize.y), new Vector2(0.5f, 0.5f), 8f);
	}

	public static Sprite ComposeSprites(Sprite[] sprites, Vector2Int[] positions, int padding = 1)
	{
		if (sprites.IsNullOrEmpty() || positions.IsNullOrEmpty())
		{
			return null;
		}
		int num = Mathf.Min(sprites.Length, positions.Length);
		RectInt[] array = new RectInt[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new RectInt(positions[i], new Vector2Int((int)sprites[i].rect.width, (int)sprites[i].rect.height));
		}
		RectInt rectInt = array.CombineRects();
		Vector2Int size = rectInt.size;
		for (int j = 0; j < num; j++)
		{
			array[j].position -= rectInt.position;
		}
		Color[] array2 = new Color[size.x * size.y];
		for (int k = 0; k < array2.Length; k++)
		{
			array2[k] = Color.clear;
		}
		for (int l = 0; l < num; l++)
		{
			WriteSpriteIntoBuffer(array2, size.x, sprites[l], new Vector2Int(array[l].x, array[l].y));
		}
		int num2 = padding * 2;
		Vector2Int bufferSize = new Vector2Int(size.x + num2, size.y + num2);
		Color[] array3 = new Color[bufferSize.x * bufferSize.y];
		for (int m = 0; m < array3.Length; m++)
		{
			array3[m] = Color.clear;
		}
		WriteColorsIntoBuffer(array3, bufferSize, array2, size, Vector2Int.one);
		Texture2D texture2D = new Texture2D(bufferSize.x, bufferSize.y);
		texture2D.filterMode = FilterMode.Point;
		texture2D.SetPixels(0, 0, bufferSize.x, bufferSize.y, array3);
		texture2D.Apply();
		return Sprite.Create(texture2D, new Rect(0f, 0f, bufferSize.x, bufferSize.y), new Vector2(0.5f, 0.5f), 8f);
		static void WriteSpriteIntoBuffer(Color[] buffer, int bufferWidth, Sprite sprite, Vector2Int position)
		{
			Vector2Int vector2Int = new Vector2Int((int)sprite.rect.width, (int)sprite.rect.height);
			Color[] spritePixels = sprite.GetSpritePixels();
			for (int n = 0; n < vector2Int.x; n++)
			{
				for (int num3 = 0; num3 < vector2Int.y; num3++)
				{
					Vector2Int vector2Int2 = position + new Vector2Int(n, num3);
					int num4 = vector2Int2.y * bufferWidth + vector2Int2.x;
					int num5 = num3 * vector2Int.x + n;
					if (spritePixels[num5].a != 0f)
					{
						buffer[num4] = spritePixels[num5];
					}
				}
			}
		}
	}

	private static void WriteColorsIntoBuffer(Color[] buffer, Vector2Int bufferSize, Color[] pixels, Vector2Int pxSize, Vector2Int position)
	{
		for (int i = 0; i < pxSize.x; i++)
		{
			for (int j = 0; j < pxSize.y; j++)
			{
				Vector2Int vector2Int = position + new Vector2Int(i, j);
				int num = vector2Int.y * bufferSize.x + vector2Int.x;
				Color color = pixels[j * pxSize.x + i];
				if (color.a != 0f)
				{
					buffer[num] = color;
				}
			}
		}
	}
}
