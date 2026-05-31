using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimFadeInoutImages : UIAnimFadeInoutGroup
{
	public UIAnimFadeInoutImages(Transform transform, Color c)
		: base(transform, c)
	{
	}

	protected override void initialize(Transform transform)
	{
		List<Action<Color>> list = new List<Action<Color>>();
		Image[] componentsInChildren = transform.GetComponentsInChildren<Image>();
		foreach (Image image in componentsInChildren)
		{
			list.Add(delegate(Color c)
			{
				image.color = c;
			});
		}
		setters = list.ToArray();
	}

	public void reload(Transform transform, int count)
	{
		Image[] images = transform.GetComponentsInChildren<Image>();
		if (count >= images.Length)
		{
			return;
		}
		List<Action<Color>> list = new List<Action<Color>>();
		int i;
		for (i = 0; i < count; i++)
		{
			list.Add(delegate(Color c)
			{
				images[i].color = c;
			});
		}
		setters = list.ToArray();
	}
}
