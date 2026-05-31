using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAnimFadeInoutTexts : UIAnimFadeInoutGroup
{
	public UIAnimFadeInoutTexts(Transform transform, Color c)
		: base(transform, c)
	{
	}

	protected override void initialize(Transform transform)
	{
		List<Action<Color>> list = new List<Action<Color>>();
		Text[] componentsInChildren = transform.GetComponentsInChildren<Text>();
		foreach (Text text in componentsInChildren)
		{
			list.Add(delegate(Color c)
			{
				text.color = c;
			});
		}
		setters = list.ToArray();
	}

	public void reload(Transform transform, int count)
	{
		Text[] texts = transform.GetComponentsInChildren<Text>();
		if (count >= texts.Length)
		{
			return;
		}
		List<Action<Color>> list = new List<Action<Color>>();
		int i;
		for (i = 0; i < count; i++)
		{
			list.Add(delegate(Color c)
			{
				texts[i].color = c;
			});
		}
		setters = list.ToArray();
	}
}
