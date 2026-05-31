using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public static class DolocPatch
{
	public static Quaternion GetRotation(this Vector2 dir)
	{
		float z = Mathf.Atan2(dir.y, dir.x) * 57.29578f;
		return Quaternion.Euler(0f, 0f, z);
	}

	public static float Between(this Vector2 range)
	{
		return UnityEngine.Random.Range(range.x, range.y);
	}

	public static Vector2 RandomOffset(this Vector2 range)
	{
		return new Vector2(UnityEngine.Random.Range(0f - range.x, range.x), UnityEngine.Random.Range(0f - range.y, range.y));
	}

	public static float Random(this Vector2 range)
	{
		if (range.x >= range.y)
		{
			return range.y;
		}
		return UnityEngine.Random.Range(range.x, range.y + 1f);
	}

	public static float Random(this Vector2 range, int seed)
	{
		if (range.x >= range.y)
		{
			return range.y;
		}
		UnityEngine.Random.InitState(seed);
		return UnityEngine.Random.Range(range.x, range.y + 1f);
	}

	public static int Random(this Vector2Int range)
	{
		if (range.x >= range.y)
		{
			return range.y;
		}
		return UnityEngine.Random.Range(range.x, range.y + 1);
	}

	public static int Random(this Vector2Int range, int seed)
	{
		if (range.x >= range.y)
		{
			return range.y;
		}
		UnityEngine.Random.InitState(seed);
		return UnityEngine.Random.Range(range.x, range.y + 1);
	}

	public static void RaiseEmotion(this Transform transform, EmotionName emotionType)
	{
		if (!(transform == null))
		{
			DolocAPI.RaiseEmotion(transform, emotionType);
		}
	}

	public static bool IsNullOrEmpty(this string str)
	{
		return string.IsNullOrEmpty(str);
	}

	public static string ExtractContentFromColorTag(this string input)
	{
		string pattern = "<color=#[\\da-fA-F]+>(.*?)<\\/color>";
		Match match = Regex.Match(input, pattern);
		if (match.Success)
		{
			return match.Groups[1].Value;
		}
		return input;
	}

	public static Color Alpha(this Color color, float a)
	{
		color.a = a;
		return color;
	}

	public static bool IsNullOrEmpty<T>(this T[] arr)
	{
		if (arr != null)
		{
			return arr.Length == 0;
		}
		return true;
	}

	public static bool IsNullOrEmpty<T>(this IEnumerable<T> arr)
	{
		if (arr != null)
		{
			return !arr.Any();
		}
		return true;
	}

	public static bool IsNotNullOrEmpty<T>(this T[] arr)
	{
		if (arr != null)
		{
			return arr.Length != 0;
		}
		return false;
	}

	public static bool IsLessThan<T>(this T[] arr, int length)
	{
		if (arr != null)
		{
			return arr.Length < length;
		}
		return false;
	}

	public static bool IsGreaterThan<T>(this T[] arr, int length)
	{
		if (arr != null)
		{
			return arr.Length > length;
		}
		return false;
	}

	public static void SetAlpha(this SpriteRenderer renderer, float alpha)
	{
		if (!(renderer == null))
		{
			renderer.color = renderer.color.Alpha(alpha);
		}
	}

	public static void SetColorWithoutChangingAlpha(this SpriteRenderer renderer, Color c)
	{
		if (!(renderer == null))
		{
			renderer.color = c.Alpha(renderer.color.a);
		}
	}

	public static void GrowToNext(this SpriteRenderer renderer, Sprite sprite, Vector2 scaleTime, Ease ease, float shakeTime, float angle)
	{
		if (!(renderer == null) && !(sprite == null))
		{
			Vector2 vector = ((renderer.sprite == null) ? Vector2.zero : (renderer.sprite.rect.size / sprite.rect.size));
			renderer.sprite = sprite;
			Transform transform = renderer.transform;
			transform.localScale = new Vector3(vector.x, vector.y, 1f);
			transform.DOScaleX(1f, scaleTime.x).SetEase(ease);
			transform.DOScaleY(1f, scaleTime.y).SetEase(ease);
			transform.DOShakeRotation(shakeTime, Vector3.forward * angle, 30);
			DolocAPI.effectProvider.RaiseInstPS(transform.position, InstantParticleEffectsType.LEAVES_AND_SOILS);
		}
	}

	public static void GrowToNext(this SpriteRenderer sr, Sprite sprite)
	{
		if (!(sr == null) && !(sprite == null))
		{
			sr.GrowToNext(sprite, new Vector2(0.2f, 0.3f), Ease.OutBack, 0.5f, 5f);
		}
	}

	public static T[] GetComponentsInScene<T>(this GameObject gameObject, bool includeInactive = false)
	{
		if (gameObject == null)
		{
			return Array.Empty<T>();
		}
		List<T> list = new List<T>();
		GameObject[] rootGameObjects = gameObject.scene.GetRootGameObjects();
		foreach (GameObject gameObject2 in rootGameObjects)
		{
			list.AddRange(gameObject2.GetComponentsInChildren<T>(includeInactive));
		}
		return list.ToArray();
	}

	public static void InvokeSafe(this Action callback)
	{
		if (callback == null)
		{
			return;
		}
		try
		{
			callback();
		}
		catch (Exception ex)
		{
			Debug.LogError("Callback Error: " + ex.Message);
			Debug.LogException(ex);
		}
	}
}
