using System;
using DG.Tweening;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterMoverDoTween : MonsterMover
{
	private readonly MonsterMoverProtoDoTween _protoDoTween;

	private Tween _tween;

	private float _duration;

	private BezierCurve2 _bezierCurve;

	public MonsterMoverDoTween(MonsterEnv env, Transform transform, MonsterMoverProtoDoTween proto, bool isAir)
		: base(env, transform, proto, isAir)
	{
		_protoDoTween = proto;
	}

	public override bool TryMoveTo(Vector2 target, Action callback = null)
	{
		base.callback = callback;
		Vector3 position = _transform.position;
		float magnitude = ((Vector2)position - target).magnitude;
		_duration = magnitude / _protoDoTween.moveSpeed;
		_bezierCurve = new BezierCurve2(control_01: new Vector2(target.x, position.y), from: position, to: target);
		float t = 0f;
		_tween = DOTween.To(() => t, delegate(float x)
		{
			t = x;
		}, 1f, Mathf.Min(3f, _duration)).SetEase(_protoDoTween.moveEase).SetUpdate(UpdateType.Manual)
			.OnUpdate(delegate
			{
				_transform.position = _bezierCurve.GetPosition(t);
			})
			.OnComplete(delegate
			{
				callback?.Invoke();
			});
		return true;
	}

	protected override bool OnUpdate(float dt)
	{
		_duration -= dt;
		if (_duration <= 0f)
		{
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}
}
