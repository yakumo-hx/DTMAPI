using DG.Tweening;
using UnityEngine;

namespace DolocTown.UI;

public abstract class UIEffects
{
	private Tween _tween;

	public void Invoke(RectTransform transform)
	{
		_tween?.Kill();
		_tween = Start(transform);
	}

	protected abstract Tween Start(RectTransform target);
}
