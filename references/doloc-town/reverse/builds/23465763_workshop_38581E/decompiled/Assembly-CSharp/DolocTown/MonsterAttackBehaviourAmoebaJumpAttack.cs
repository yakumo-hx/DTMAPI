using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourAmoebaJumpAttack : MonsterAttackBehaviourPhysical
{
	private new readonly IAmoebaJumpAttack _proto;

	private readonly Animator _animator;

	private BezierCurveParabolic _curve;

	private Tween _tween;

	private bool _isReady;

	private float _readyElapsed;

	private bool _valid;

	private IGameMap GameMap => _controller.Env.GetMap(isAir: false);

	public MonsterAttackBehaviourAmoebaJumpAttack(Transform host, IAmoebaJumpAttack proto)
		: base(host, proto)
	{
		_proto = proto;
		_animator = host.GetComponent<Animator>();
		DisableDamageBox();
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return Vector2.Distance(target.position, host.transform.position) < _proto.AttackRange;
	}

	public override bool OnUpdate(float dt)
	{
		if (!_valid)
		{
			return true;
		}
		if (_isReady)
		{
			_readyElapsed += dt;
			if (_readyElapsed < _proto.ReadyDuration)
			{
				return false;
			}
			_readyElapsed = 0f;
			_isReady = false;
			if (!GetAttackPointEx(base.target, out var result))
			{
				return true;
			}
			JumpAttack(result);
			return false;
		}
		if (_tween == null)
		{
			AnimatorStateInfo currentAnimatorStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
			if (!currentAnimatorStateInfo.IsName("touch_ground"))
			{
				return true;
			}
			if (!(currentAnimatorStateInfo.normalizedTime >= 1f))
			{
				return false;
			}
			End();
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}

	public override void Invoke()
	{
		_valid = false;
		Begin();
		_valid = true;
		_controller.RaiseDangerWarning01();
		host.localScale = new Vector3(Mathf.Sign(host.transform.position.x - base.target.position.x), 1f, 1f);
		_animator.Play("jump_ready");
		_isReady = true;
	}

	private bool GetAttackPointEx(Transform target, out Vector2 result)
	{
		result = default(Vector2);
		Vector3 position = _controller.transform.position;
		IGameMap map = _controller.Env.GetMap(isAir: false);
		Vector2Int pos = map.WorldToCell(position);
		Vector2Int[] jumpPoints = map.GetJumpPoints(pos, 4, 4);
		Vector2[] array = map.CellToWorldPivot(jumpPoints, new Vector2(0.5f, 0f));
		if (array.IsNullOrEmpty())
		{
			return false;
		}
		Vector2 vector = NearestPoint(array, target.transform.position);
		result = vector;
		return true;
	}

	private Vector2 NearestPoint(Vector2[] points, Vector2 current)
	{
		if (points.Length == 1)
		{
			return points[0];
		}
		int num = 0;
		float num2 = Vector2.Distance(points[0], current);
		for (int i = 1; i < points.Length; i++)
		{
			float num3 = Vector2.Distance(current, points[i]);
			if (!(num3 >= num2))
			{
				num2 = num3;
				num = i;
			}
		}
		return points[num];
	}

	private bool GetAttackPoint(Transform target, out Vector2 result)
	{
		result = default(Vector2);
		Vector3 position = target.position;
		position.y += 0.5f;
		if (GameMap.RaycastToGround(target.position, out result))
		{
			Vector3 position2 = host.position;
			if (Mathf.Abs(result.y - position2.y) <= _proto.MaxJumpHeight && Vector2.Distance(result, position2) <= _proto.AttackRange)
			{
				return true;
			}
		}
		Vector3 position3 = host.position;
		position3.x -= host.localScale.x * _proto.AttackRange;
		return GameMap.RaycastToGround(position3, out result);
	}

	private void JumpAttack(Vector2 targetPosition)
	{
		EnableDamageBox(_proto.Damage, _proto.CriticalRate);
		_animator.Play("jump");
		Vector2 vector = host.position;
		ResetDirection(targetPosition);
		_curve = new BezierCurveParabolic(vector, targetPosition);
		float t = 0f;
		float duration = Vector2.Distance(vector, targetPosition) / _proto.JumpSpeed;
		_tween = DOTween.To(() => t, delegate(float v)
		{
			t = v;
		}, 1f, duration).OnUpdate(delegate
		{
			host.position = _curve.GetPositionEx(t);
			if (_animator.GetCurrentAnimatorStateInfo(0).IsName("jump") && t >= 0.5f)
			{
				_animator.Play("drop");
			}
		}).OnComplete(Stop)
			.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
	}

	private void ResetDirection(Vector2 target)
	{
		if (host.transform.position.x - target.x == 0f)
		{
			host.localScale = Vector3.one;
		}
		else
		{
			host.localScale = new Vector3(Mathf.Sign(host.transform.position.x - target.x), 1f, 1f);
		}
	}

	private void Stop()
	{
		DisableDamageBox();
		_animator.Play("touch_ground");
		_tween?.Kill();
		_tween = null;
	}
}
