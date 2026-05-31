using DG.Tweening;
using UnityEngine;

public class UIAnimFadeInoutGroupEx : UIAnimFadeInout
{
	private UIAnimFadeInoutImages imageHandler;

	private UIAnimFadeInoutTexts textHandler;

	public float alphaText;

	public Color ImageColor
	{
		get
		{
			return imageHandler.CurrentColor;
		}
		set
		{
			imageHandler.CurrentColor = value;
		}
	}

	public Color TextColor
	{
		get
		{
			return textHandler.CurrentColor;
		}
		set
		{
			textHandler.CurrentColor = value;
		}
	}

	public float ImageAlpha
	{
		get
		{
			return imageHandler.alpha;
		}
		set
		{
			imageHandler.Alpha = value;
		}
	}

	public float TextAlpha
	{
		get
		{
			return textHandler.alpha;
		}
		set
		{
			textHandler.Alpha = value;
		}
	}

	public UIAnimFadeInoutGroupEx(Transform transform, Color imageColor, Color textColor)
	{
		imageHandler = new UIAnimFadeInoutImages(transform, imageColor);
		textHandler = new UIAnimFadeInoutTexts(transform, textColor);
	}

	public UIAnimFadeInoutGroupEx(Transform transform, Color imageColor)
	{
		imageHandler = new UIAnimFadeInoutImages(transform, imageColor);
		textHandler = new UIAnimFadeInoutTexts(transform, new Color(1f, 1f, 0.92156f));
	}

	public void reload(Transform transform, int count)
	{
		imageHandler.reload(transform, count);
		textHandler.reload(transform, count);
	}

	public override Tween buildAnim()
	{
		Sequence sequence = DOTween.Sequence();
		sequence.Join(imageHandler.buildAnim(base.alpha, base.time, base.ease));
		sequence.Join(textHandler.buildAnim(alphaText, base.time, base.ease));
		return sequence;
	}

	public override void setAlphaImmediately(float value)
	{
		ImageAlpha = value;
		TextAlpha = value;
	}
}
