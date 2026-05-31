using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public static class DolocResources
{
	public static RuntimeAnimatorController defaultNpcController => Resources.Load<RuntimeAnimatorController>("defaultNpc");

	public static bool readConfig(string name, out string source)
	{
		TextAsset textAsset = Resources.Load<TextAsset>(name);
		if (textAsset == null)
		{
			source = string.Empty;
			return false;
		}
		source = textAsset.text;
		return true;
	}

	public static string getConfigSource(string name)
	{
		TextAsset textAsset = Resources.Load<TextAsset>(name);
		if (textAsset == null)
		{
			return null;
		}
		return textAsset.text;
	}

	public static bool loadConfig(string path, out string source)
	{
		TextAsset textAsset = Resources.Load<TextAsset>(path);
		if (textAsset == null)
		{
			source = null;
			return false;
		}
		source = textAsset.text;
		return true;
	}

	public static bool loadSpriteAtlas(string path, out Dictionary<string, Sprite> lut, Action<string> log = null)
	{
		SpriteAtlas spriteAtlas = Resources.Load<SpriteAtlas>(path);
		if (spriteAtlas != null)
		{
			Sprite[] array = new Sprite[spriteAtlas.spriteCount];
			if (spriteAtlas.GetSprites(array) != array.Length)
			{
				log?.Invoke("图集<" + path + ">数量与获取的数量不对等,这有可能造成问题");
			}
			lut = new Dictionary<string, Sprite>();
			Sprite[] array2 = array;
			foreach (Sprite sprite in array2)
			{
				if (lut.ContainsKey(sprite.name))
				{
					log?.Invoke("图集<" + path + ">重名要素:" + sprite.name);
				}
				else
				{
					lut.Add(sprite.name, sprite);
				}
			}
			return true;
		}
		lut = null;
		return false;
	}

	public static bool loadSpriteAtlas(string path, out Dictionary<string, Sprite> lut, Func<string, string> handleKey, Action<string> log = null)
	{
		SpriteAtlas spriteAtlas = Resources.Load<SpriteAtlas>(path);
		if (spriteAtlas != null)
		{
			Sprite[] array = new Sprite[spriteAtlas.spriteCount];
			if (spriteAtlas.GetSprites(array) != array.Length)
			{
				log?.Invoke("图集<" + path + ">数量与获取的数量不对等,这有可能造成问题");
			}
			lut = new Dictionary<string, Sprite>();
			Sprite[] array2 = array;
			foreach (Sprite sprite in array2)
			{
				if (lut.ContainsKey(sprite.name))
				{
					log?.Invoke("图集<" + path + ">重名要素:" + sprite.name);
				}
				else
				{
					lut.Add(handleKey(sprite.name), sprite);
				}
			}
			return true;
		}
		lut = null;
		return false;
	}
}
