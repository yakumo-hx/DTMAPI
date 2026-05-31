using System;
using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public abstract class MonsterAttackBehaviour
{
	public readonly IMonsterAttackBehaviour _proto;

	protected readonly Transform host;

	protected readonly MonsterController _controller;

	protected Transform target { get; private set; }

	protected bool IsDirectional => _controller.Monster.proto.MoverProto.isDirectional;

	public float CDDuration => _proto.CDDuration;

	public bool CDOnStart => _proto.CDOnStart;

	protected Vector2 DirToTarget => _controller.DirToTarget(target);

	protected Vector2 DirToTargetHorizontal => new Vector2(Mathf.Sign(DirToTarget.x), 0f);

	protected MonsterAttackBehaviour(Transform host, IMonsterAttackBehaviour proto)
	{
		this.host = host;
		_proto = proto;
		_controller = host.GetComponent<MonsterController>();
	}

	public void SetAttackTarget(Transform target)
	{
		this.target = target;
	}

	protected void Begin()
	{
		host.GetComponent<MonsterController>().Renderer.OnAttackBegin(_proto.AttackId, GetType(), target);
	}

	protected void End()
	{
		host.GetComponent<MonsterController>().Renderer.OnAttackEnd(_proto.AttackId, GetType(), target);
	}

	protected Tween DoReadyAction(float readyDuration, Action callback = null, bool shouldFaceTarget = true)
	{
		Vector2 vector = host.position;
		Vector2 normalized = ((Vector2)target.position - vector).normalized;
		return DoReadyAction(normalized, readyDuration, callback, shouldFaceTarget);
	}

	protected Tween DoReadyAction(Vector2 shootDir, float readyDuration, Action callback, bool shouldFaceTarget = true)
	{
		Vector2 pos = (Vector2)host.position - shootDir * 1.5f;
		Tween tween = MonsterUtils.MoveTo(host, pos, readyDuration, Ease.OutExpo, callback, shouldFaceTarget);
		tween.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		return tween;
	}

	protected Tween Recoil(Action<Vector2> shootAction, float duration = 0.35f, float recoilDistance = 1.2f, float recoilDurationRatio = 0.3f, bool shouldFaceTarget = true)
	{
		Vector2 vector = host.position;
		Vector2 shootDir = DirToTarget;
		Vector2 vector2 = vector - shootDir * recoilDistance;
		float num = duration * recoilDurationRatio;
		float duration2 = duration - num;
		if (shouldFaceTarget)
		{
			host.localScale = MonsterUtils.GetScaleFromShootDir(shootDir.x);
		}
		Sequence sequence = DOTween.Sequence();
		sequence.AppendCallback(delegate
		{
			shootAction(shootDir);
		});
		sequence.Append(host.DOMove(vector2, num).SetEase(Ease.OutExpo));
		sequence.Append(host.DOMove(vector, duration2).SetEase(Ease.OutBack));
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		return sequence;
	}

	protected Tween DoRecoil(Vector2 shootDir, TweenCallback callback, float duration = 0.35f, float recoilDistance = 1.2f, float recoilDurationRatio = 0.3f)
	{
		Vector2 vector = host.position;
		Vector2 vector2 = vector - shootDir * recoilDistance;
		float num = duration * recoilDurationRatio;
		float duration2 = duration - num;
		Sequence sequence = DOTween.Sequence();
		sequence.AppendCallback(callback);
		sequence.Append(host.DOMove(vector2, num).SetEase(Ease.OutExpo));
		sequence.Append(host.DOMove(vector, duration2).SetEase(Ease.OutBack));
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		return sequence;
	}

	public abstract bool OnUpdate(float dt);

	public virtual bool Validate()
	{
		return true;
	}

	public virtual bool IsTargetInAttackRange(Transform target)
	{
		return false;
	}

	public virtual void Invoke()
	{
	}

	public virtual void BeforeInvoke()
	{
	}

	public abstract void Dispose(BattleSystem bs);
}
