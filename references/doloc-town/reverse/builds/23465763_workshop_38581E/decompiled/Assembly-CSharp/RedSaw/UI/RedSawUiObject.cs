using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.UI;

public abstract class RedSawUiObject : RedSawObject
{
	private static Action<string> errCallback;

	private static bool hasInitialized;

	private static List<RedSawUiObject> instances;

	private static Vector2 realResolution;

	private static Vector2 resolution;

	public static Vector2 resolutionRatio { get; private set; }

	public static Vector2 Resolution
	{
		get
		{
			return resolution;
		}
		set
		{
			resolution = value;
			resolutionRatio = resolution / realResolution;
			if (!hasInitialized)
			{
				return;
			}
			foreach (RedSawUiObject instance in instances)
			{
				instance.onResolutionChanged();
			}
		}
	}

	public RectTransform rectTransform { get; private set; }

	public Vector2 scaledSize => rectTransform.sizeDelta * rectTransform.localScale;

	public Vector2 size
	{
		get
		{
			return rectTransform.sizeDelta;
		}
		set
		{
			rectTransform.sizeDelta = value;
		}
	}

	public Vector2 position
	{
		get
		{
			return rectTransform.localPosition;
		}
		set
		{
			rectTransform.localPosition = value;
		}
	}

	public static Vector2 getSpriteSize(Sprite sprite)
	{
		if (sprite == null)
		{
			return Vector2.one;
		}
		return sprite.rect.size * resolutionRatio;
	}

	public static void resizeImage(Image image)
	{
		if (!(image == null))
		{
			image.GetComponent<RectTransform>().sizeDelta = getSpriteSize(image.sprite);
		}
	}

	public static void initRedSawsUiSystem(Vector2 realResolution)
	{
		if (!hasInitialized)
		{
			hasInitialized = true;
			instances = new List<RedSawUiObject>();
			RedSawUiObject.realResolution = realResolution;
			errCallback = Debug.LogWarning;
		}
	}

	public static void resetErrCallback(Action<string> errCallback)
	{
		RedSawUiObject.errCallback = errCallback;
	}

	public static void destory(RedSawUiObject obj)
	{
		if (!(obj == null))
		{
			if (instances.Contains(obj))
			{
				instances.Remove(obj);
				UnityEngine.Object.DestroyImmediate(obj);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(obj);
				errCallback("错误报告:存在非法出现的RedSawUiObject");
			}
		}
	}

	public override void OnCreated()
	{
		rectTransform = GetComponent<RectTransform>();
		if (hasInitialized)
		{
			instances.Add(this);
		}
	}

	public void setParent(Transform parent)
	{
		rectTransform.SetParent(parent);
	}

	protected virtual void onResolutionChanged()
	{
	}
}
