using DG.Tweening;
using DolocTown.GameServiceLocator;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class UiEffectsManager : IUIEffectsProvider
{
	private FadeUpTipTextManager _fadeUpTipTextManager;

	private FadeUpTipSpriteManager _fadeUpTipSpriteManager;

	private readonly FadeUpTipNumberManager _fadeUpTipNumberManager;

	private DamageTipManager _damageTipManager;

	public UiEffectsManager(Transform container)
	{
		_fadeUpTipTextManager = new FadeUpTipTextManager();
		_fadeUpTipSpriteManager = new FadeUpTipSpriteManager();
		_fadeUpTipNumberManager = new FadeUpTipNumberManager();
		_damageTipManager = new DamageTipManager();
	}

	public void RaiseDamageTip(int count, Vector2 ws, bool isHeavy, float duration, float popDistance, float waitTime)
	{
		_damageTipManager.Raise(count, ws, isHeavy, duration, popDistance, waitTime);
	}

	public void RaiseFadeUpSprite(Vector2 screenPos, Sprite icon, Ease ease, float duration, float popDistance)
	{
		_fadeUpTipSpriteManager.RaiseFadeUpSprite(screenPos, icon, ease, duration, popDistance, inScene: false);
	}

	public void RaiseFadeDownSprite(Vector2 screenPos, Sprite icon, Ease ease, float duration, float popDistance)
	{
		_fadeUpTipSpriteManager.RaiseFadeDownSprite(screenPos, icon, ease, duration, popDistance, inScene: false);
	}

	public void RaiseFadeUpText(string text, Color color, Vector2 screenPos, Ease ease, float duration, float popDistance)
	{
		_fadeUpTipTextManager.RaiseFadeUpText(text, color, screenPos, ease, duration, popDistance);
	}

	public void RaiseFadeUpNumber(int value, Color color, Vector2 screenPos, Ease ease, float duration, float popDistance, float waitDuration)
	{
		_fadeUpTipNumberManager.RaiseFadeUpNumber(value, color, screenPos, ease, duration, popDistance, waitDuration);
	}
}
