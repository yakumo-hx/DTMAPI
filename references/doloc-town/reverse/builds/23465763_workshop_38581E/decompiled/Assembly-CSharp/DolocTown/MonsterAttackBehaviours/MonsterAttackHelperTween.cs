using System;
using DG.Tweening;

namespace DolocTown.MonsterAttackBehaviours;

public class MonsterAttackHelperTween
{
	private readonly Action _stopCallback;

	private Tween _tween;

	public MonsterAttackHelperTween(Action stopCallback)
	{
		_stopCallback = stopCallback;
	}

	public bool Update(float dt)
	{
		if (_tween == null)
		{
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}

	public Tween SetTween(Tween tween)
	{
		tween?.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		_tween = tween;
		return tween;
	}

	public void SetFinalTween(Tween tween)
	{
		tween?.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		_tween = tween;
		_tween.OnComplete(Stop);
	}

	public void SetFinalTween(Tween tween, Action externalCallback)
	{
		tween?.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		_tween = tween;
		_tween.OnComplete(delegate
		{
			Stop();
			externalCallback?.Invoke();
		});
	}

	public void Stop()
	{
		_stopCallback?.Invoke();
		_tween?.Kill();
		_tween = null;
	}
}
