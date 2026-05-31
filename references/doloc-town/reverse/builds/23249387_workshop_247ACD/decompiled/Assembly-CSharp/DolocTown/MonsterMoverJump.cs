using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class MonsterMoverJump
{
	private readonly struct BezierCurveParabolic
	{
		private readonly Vector2 from;

		private readonly Vector2 to;

		private readonly Vector2 control1;

		private readonly Vector2 control2;

		private static Vector2 GetPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			t = Mathf.Clamp01(t);
			float num = 1f - t;
			return num * num * num * p0 + 3f * num * num * t * p1 + 3f * num * t * t * p2 + t * t * t * p3;
		}

		public BezierCurveParabolic(Vector2 from, Vector2 to, float controlDst = 0.24f, float controlHeight = 2f)
		{
			this.from = from;
			this.to = to;
			float num = to.x - from.x;
			float y = Mathf.Max(from.y, to.y) + controlHeight;
			control1 = new Vector2(from.x + num * controlDst, y);
			control2 = new Vector2(to.x - num * controlDst, y);
		}

		private Vector2 GetPosition(float t)
		{
			return GetPoint(from, control1, control2, to, t);
		}

		public Vector2 GetPositionEx(float t)
		{
			Vector2 position = GetPosition(t);
			return new Vector2((to.x - from.x) * t + from.x, position.y);
		}
	}

	private readonly Transform _transform;

	private readonly IMonsterMoverGroundRenderer _renderer;

	private readonly float _controlDst;

	private readonly float _heightScale;

	private readonly float _speed;

	private BezierCurveParabolic _curve;

	private Tween _tween;

	private float _duration;

	private bool _isReady;

	private bool _isDrop;

	private bool _isOver;

	private Vector2 _target;

	private Vector2 position => _transform.position;

	public MonsterMoverJump(Transform transform, float speed = 12f, float controlDst = 0.24f, float heightScale = 0.3f)
	{
		_transform = transform;
		_renderer = transform.GetComponent<IMonsterMoverGroundRenderer>();
		_heightScale = heightScale;
		_controlDst = controlDst;
		_speed = speed;
		if (_speed == 0f)
		{
			Debug.LogError("Speed can't be zero");
			speed = 12f;
		}
	}

	public void JumpTo(Vector2 target)
	{
		float controlHeight = Mathf.Abs(target.y - position.y) * _heightScale;
		_curve = new BezierCurveParabolic(_transform.position, target, _controlDst, controlHeight);
		_renderer.OnReadyJump();
		_isReady = true;
		_isDrop = false;
		_isOver = false;
		_target = target;
	}

	private void StartJump(Vector2 target)
	{
		float t = 0f;
		_duration = Vector2.Distance(position, target) / _speed;
		_renderer.OnJump();
		_tween = DOTween.To(() => t, delegate(float v)
		{
			t = v;
		}, 1f, _duration).SetEase(Ease.Linear).OnUpdate(delegate
		{
			_transform.position = _curve.GetPositionEx(t);
			if (t >= 0.5f && !_isDrop)
			{
				_renderer.OnDrop();
				_isDrop = true;
			}
		})
			.OnComplete(delegate
			{
				_renderer.OnTouchGround();
				_isOver = true;
			})
			.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
	}

	public bool Update(float dt)
	{
		if (_isReady)
		{
			if (!_renderer.IsJumpReadyDone())
			{
				return false;
			}
			StartJump(_target);
			_isReady = false;
			return false;
		}
		if (_isOver)
		{
			return _renderer.IsTouchGroundDone();
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}
}
