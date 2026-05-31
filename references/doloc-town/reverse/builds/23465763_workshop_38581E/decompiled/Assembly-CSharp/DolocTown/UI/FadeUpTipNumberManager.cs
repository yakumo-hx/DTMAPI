using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public class FadeUpTipNumberManager
{
	public void RaiseFadeUpNumber(int value, Color color, Vector2 pos, Ease ease, float duration, float popDistance, float waitDuration)
	{
		_RaiseFadeUpNumber(value, color, pos, ease, duration, popDistance, waitDuration);
	}

	private void _RaiseFadeUpNumber(int value, Color color, Vector2 pos, Ease ease, float duration, float popDistance, float waitDuration)
	{
		DolocTextTMP text = DolocAPI.uiSystem.GetFromPoolInScene<DolocTextTMP>();
		text.Text = ((value >= 0) ? "+" : "-") + value;
		text.Color = color.Alpha(0f);
		text.position = pos;
		popDistance /= DolocAPI.screenManager.scaleFactor;
		float num = popDistance * 0.5f;
		float duration2 = duration * 0.5f;
		Sequence sequence = DOTween.Sequence();
		if (Time.timeScale != 0f)
		{
			sequence.timeScale = 1f / Time.timeScale;
		}
		sequence.Join(text.transform.DOMoveY(pos.y + num, duration).SetEase(ease));
		sequence.Join(text.DOFade(1f, duration2));
		sequence.AppendInterval(waitDuration);
		sequence.Join(text.DOFade(0f, duration2));
		sequence.Join(text.transform.DOMoveY(pos.y + popDistance, duration).SetEase(ease));
		sequence.OnComplete(delegate
		{
			DolocAPI.uiSystem.RecycleToPoolInScene(text);
		});
	}
}
