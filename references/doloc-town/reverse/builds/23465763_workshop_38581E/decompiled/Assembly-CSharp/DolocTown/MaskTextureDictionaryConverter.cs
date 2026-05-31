using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class MaskTextureDictionaryConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(Dictionary<string, Texture2D>);
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		Dictionary<string, Texture2D> obj = (value as Dictionary<string, Texture2D>) ?? throw new JsonSerializationException("Expected Dictionary<string, Texture2D>");
		Dictionary<string, byte[,]> dictionary = new Dictionary<string, byte[,]>();
		foreach (KeyValuePair<string, Texture2D> item in obj)
		{
			Texture2D value2 = item.Value;
			byte[,] array = new byte[value2.width, value2.height];
			Color[] pixels = value2.GetPixels();
			for (int i = 0; i < value2.height; i++)
			{
				for (int j = 0; j < value2.width; j++)
				{
					Color color = pixels[i * value2.width + j];
					array[j, i] = ((color.a > 0f) ? ((byte)1) : ((byte)0));
				}
			}
			dictionary[item.Key] = array;
		}
		serializer.Serialize(writer, dictionary);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		Dictionary<string, byte[,]>? obj = serializer.Deserialize<Dictionary<string, byte[,]>>(reader) ?? throw new JsonSerializationException("Expected Dictionary<string, byte[,]>");
		Dictionary<string, Texture2D> dictionary = new Dictionary<string, Texture2D>();
		foreach (KeyValuePair<string, byte[,]> item in obj)
		{
			byte[,] value = item.Value;
			int length = value.GetLength(0);
			int length2 = value.GetLength(1);
			Texture2D texture2D = TextureUtils.CreateTexture(length, length2);
			Color[] pixels = texture2D.GetPixels();
			for (int i = 0; i < length2; i++)
			{
				for (int j = 0; j < length; j++)
				{
					if (value[j, i] == 1)
					{
						pixels[i * length + j] = Color.white;
					}
				}
			}
			texture2D.SetPixels(pixels);
			texture2D.Apply();
			dictionary[item.Key] = texture2D;
		}
		return dictionary;
	}
}
