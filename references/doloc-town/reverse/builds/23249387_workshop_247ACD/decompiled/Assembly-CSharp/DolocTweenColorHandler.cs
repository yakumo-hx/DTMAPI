using DG.Tweening.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DolocTweenColorHandler
{
	public readonly DOGetter<Color> colorGetter;

	public readonly DOSetter<Color> colorSetter;

	public DolocTweenColorHandler(Text w)
	{
		colorGetter = () => w.color;
		colorSetter = delegate(Color c)
		{
			w.color = c;
		};
	}

	public DolocTweenColorHandler(Image w)
	{
		colorGetter = () => w.color;
		colorSetter = delegate(Color c)
		{
			w.color = c;
		};
	}

	public DolocTweenColorHandler(TMP_Text w)
	{
		colorGetter = () => w.color;
		colorSetter = delegate(Color c)
		{
			w.color = c;
		};
	}

	public DolocTweenColorHandler(DOGetter<Color> getter, DOSetter<Color> setter)
	{
		colorGetter = getter;
		colorSetter = setter;
	}
}
