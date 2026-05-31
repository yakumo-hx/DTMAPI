using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class Shaker
{
	private readonly Transform transform;

	private Vector3 originalPos;

	private Tween tween;

	public Shaker(Transform transform)
	{
		this.transform = transform;
		tween = null;
	}

	public void Shake(float duration = 0.1f, float strength = 0.5f, int vibrato = 10)
	{
		if (tween != null)
		{
			tween.Kill();
			tween = null;
			transform.position = originalPos;
		}
		originalPos = transform.position;
		tween = ShortcutExtensions.DOShakePosition(strength: new Vector3(strength, strength, 0f), target: transform, duration: duration, vibrato: vibrato);
		tween.OnComplete(delegate
		{
			tween = null;
			transform.position = originalPos;
		});
	}
}
