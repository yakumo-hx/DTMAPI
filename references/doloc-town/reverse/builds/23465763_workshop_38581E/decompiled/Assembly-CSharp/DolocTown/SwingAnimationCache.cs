using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class SwingAnimationCache
{
	private const string touchSwingLightPrefix = "swing_touch_light_";

	private const string touchSwingPrefix = "swing_touch_";

	private const string windSwingPrefix = "swing_wind_";

	private readonly string[] touchSwingNames;

	private readonly string[] touchSwingLightNames;

	private readonly string[] windSwingNames;

	public string RandomTouchSwingName
	{
		get
		{
			if (touchSwingNames.Length == 0)
			{
				return null;
			}
			if (touchSwingNames.Length == 1)
			{
				return touchSwingNames[0];
			}
			return touchSwingNames[UnityEngine.Random.Range(0, touchSwingNames.Length)];
		}
	}

	public string RandomTouchSwingLightName
	{
		get
		{
			if (touchSwingLightNames.Length == 0)
			{
				return null;
			}
			if (touchSwingLightNames.Length == 1)
			{
				return touchSwingLightNames[0];
			}
			return touchSwingLightNames[UnityEngine.Random.Range(0, touchSwingLightNames.Length)];
		}
	}

	public string RandomWindSwingName
	{
		get
		{
			if (windSwingNames.Length == 0)
			{
				return null;
			}
			if (windSwingNames.Length == 1)
			{
				return windSwingNames[0];
			}
			return windSwingNames[UnityEngine.Random.Range(0, windSwingNames.Length)];
		}
	}

	public SwingAnimationCache()
	{
		touchSwingNames = Array.Empty<string>();
		touchSwingLightNames = Array.Empty<string>();
		windSwingNames = Array.Empty<string>();
	}

	public SwingAnimationCache(RuntimeAnimatorController controller)
	{
		if (controller == null)
		{
			return;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		AnimationClip[] animationClips = controller.animationClips;
		foreach (AnimationClip animationClip in animationClips)
		{
			if (animationClip.name.StartsWith("swing_touch_light_"))
			{
				list2.Add(animationClip.name);
			}
			else if (animationClip.name.StartsWith("swing_touch_"))
			{
				list.Add(animationClip.name);
			}
			else if (animationClip.name.StartsWith("swing_wind_"))
			{
				list3.Add(animationClip.name);
			}
		}
		touchSwingNames = list.ToArray();
		touchSwingLightNames = list2.ToArray();
		windSwingNames = list3.ToArray();
	}
}
