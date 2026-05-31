using DG.Tweening;
using UnityEngine;

namespace DolocTown.GameServiceLocator;

public interface IUIEffectsProvider
{
	void RaiseFadeUpText(string text, Color color, Vector2 screenPos, Ease ease, float duration, float popDistance);

	void RaiseFadeUpNumber(int value, Color color, Vector2 screenPos, Ease ease, float duration, float popDistance, float waitDuration);

	void RaiseFadeUpSprite(Vector2 screenPos, Sprite icon, Ease ease, float duration, float popDistance);

	void RaiseFadeDownSprite(Vector2 screenPos, Sprite icon, Ease ease, float duration, float popDistance);

	void RaiseDamageTip(int count, Vector2 screenPos, bool isHeavy, float duration, float popDistance, float waitTime);
}
