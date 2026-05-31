using DG.Tweening;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class RotateImage : DolocUiObject
{
	[SerializeField]
	private float rotateInterval = 2f;

	[SerializeField]
	private Ease rotateEase = Ease.Linear;

	[SerializeField]
	private float rotateDuration = 1f;

	[SerializeField]
	private float rotateAngle = 10f;

	[SerializeField]
	private int vibrato = 5;

	[SerializeField]
	private float elasticity = 2f;

	private Tween _tween;

	private RSTimer _timer;

	private bool IsRotate
	{
		get
		{
			if (_tween != null)
			{
				return _tween.IsPlaying();
			}
			return false;
		}
	}

	private void Start()
	{
		_timer = new RSTimer(rotateInterval);
	}

	private void Rotate()
	{
		if (_tween == null)
		{
			_tween = base.transform.DOPunchRotation(new Vector3(0f, 0f, rotateAngle), rotateDuration, vibrato, rotateDuration).SetEase(rotateEase).OnComplete(delegate
			{
				_tween = null;
				_timer.SetInterval(rotateInterval);
			});
		}
	}

	private void Update()
	{
		if (!IsRotate && _timer.Tick(Time.deltaTime))
		{
			Rotate();
		}
	}
}
