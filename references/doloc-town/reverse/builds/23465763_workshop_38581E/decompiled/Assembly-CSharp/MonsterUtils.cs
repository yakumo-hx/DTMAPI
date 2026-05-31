using System;
using System.Collections.Generic;
using DG.Tweening;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Monster;
using UnityEngine;

public static class MonsterUtils
{
	private static Dictionary<string, MonsterType> _monsterTypeCache;

	public static MonsterType GetMonsterType(string monsterName)
	{
		if (_monsterTypeCache == null)
		{
			_monsterTypeCache = new Dictionary<string, MonsterType>();
			foreach (MonsterDocumentInfo value in DolocConfig.Tables.TbMonsterDocument.DataMap.Values)
			{
				_monsterTypeCache[value.Id] = value.MonsterType;
			}
		}
		return _monsterTypeCache.GetValueOrDefault(monsterName);
	}

	public static void RaiseInstAnim(this MonsterController controller, InstAnimEffectType type)
	{
		DolocAPI.RaiseInstantAnimEffects(controller.EmotionPos, type, Vector2.zero, flip: false, LocMaterials.GAME_MAT_2D_UNLIT, "SceneUI");
	}

	public static void RaiseDangerWarning02(this MonsterController controller)
	{
		controller.RaiseInstAnim(InstAnimEffectType.DANGER_WARNING_02);
	}

	public static void RaiseDangerWarning01(this MonsterController controller)
	{
		controller.RaiseInstAnim(InstAnimEffectType.DANGER_WARNING_01);
	}

	public static Vector3 GetScaleFromMoveDir(float x)
	{
		return new Vector3((!(x < 0f)) ? 1 : (-1), 1f, 1f);
	}

	public static Vector3 GetScaleFromShootDir(float x)
	{
		return new Vector3((!(x > 0f)) ? 1 : (-1), 1f, 1f);
	}

	private static Vector2 _GetPosition(Transform target)
	{
		if (target == null)
		{
			return Vector2.zero;
		}
		Collider2D component = target.GetComponent<Collider2D>();
		if (component == null)
		{
			return target.position;
		}
		return component.bounds.center;
	}

	public static Vector2 DirToTarget(this MonsterController controller, Transform target)
	{
		if (controller == null || target == null)
		{
			return Vector2.zero;
		}
		Vector2 firePosition = controller.FirePosition;
		return (_GetPosition(target) - firePosition).normalized;
	}

	public static float DistanceToTarget(this MonsterController controller, Transform target)
	{
		if (controller == null || target == null)
		{
			return float.MaxValue;
		}
		Vector2 firePosition = controller.FirePosition;
		return (_GetPosition(target) - firePosition).magnitude;
	}

	public static bool RayTest(this MonsterController controller, Transform target, float distance)
	{
		Vector2 firePosition = controller.FirePosition;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(firePosition, (_GetPosition(target) - firePosition).normalized, distance, DolocAPI.gameConfig.EnemyFireTestMask);
		if (raycastHit2D.collider != null)
		{
			return raycastHit2D.collider.transform == target;
		}
		return false;
	}

	public static Tween MoveTo(Transform host, Vector2 pos, float duration, Ease ease, Action callback = null, bool shouldUpdateDirection = false)
	{
		if (shouldUpdateDirection)
		{
			host.localScale = GetScaleFromMoveDir(pos.x - host.position.x);
		}
		return host.DOMove(pos, duration).SetEase(ease).OnComplete(delegate
		{
			callback?.Invoke();
		})
			.SetUpdate(UpdateType.Manual);
	}

	public static Tween Recoil(this MonsterController controller, Transform target, Action<Vector2> shootAction, float duration = 0.35f, float recoilDistance = 1.2f, float recoilDurationRatio = 0.3f, bool shouldFaceTarget = true)
	{
		Vector2 vector = controller.position;
		Vector2 shootDir = controller.DirToTarget(target);
		Vector2 vector2 = vector - shootDir * recoilDistance;
		float num = duration * recoilDurationRatio;
		float duration2 = duration - num;
		if (shouldFaceTarget)
		{
			controller.transform.localScale = GetScaleFromShootDir(shootDir.x);
		}
		Sequence sequence = DOTween.Sequence();
		sequence.AppendCallback(delegate
		{
			shootAction(shootDir);
		});
		sequence.Append(controller.transform.DOMove(vector2, num).SetEase(Ease.OutExpo));
		sequence.Append(controller.transform.DOMove(vector, duration2).SetEase(Ease.OutBack));
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		return sequence;
	}

	public static Tween RecoilFront(Transform host, Vector2 shootDir, float strength = 1.5f, float duration = 0.8f, Action callback = null)
	{
		Vector2 pos = (Vector2)host.position - shootDir * strength;
		return MoveTo(host, pos, duration * 0.3f, Ease.OutExpo, delegate
		{
			callback?.Invoke();
		});
	}

	public static void RecoilResume(Transform host, Vector2 sourcePos, float duration = 0.75f, Action callback = null)
	{
		MoveTo(host, sourcePos, duration, Ease.OutBack, callback);
	}

	public static Tween Recoil(Transform transform, Vector2 shootDir, float duration = 0.75f, float strength = 1.2f, Action callback = null, bool shouldFaceShootDir = false)
	{
		Vector2 vector = transform.position;
		if (shouldFaceShootDir)
		{
			transform.localScale = GetScaleFromShootDir(shootDir.x);
		}
		Sequence sequence = DOTween.Sequence();
		sequence.Append(transform.DOMove(vector + -shootDir * strength, duration * 0.3f).SetEase(Ease.OutExpo));
		sequence.Append(transform.DOMove(vector, duration * 0.7f).SetEase(Ease.OutBack));
		sequence.OnComplete(delegate
		{
			callback?.Invoke();
		});
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		return sequence;
	}

	public static void Wait(float preTime = 1f, Action callback = null)
	{
		DOVirtual.DelayedCall(preTime, delegate
		{
			callback?.Invoke();
		});
	}

	public static void FireRecoil(SpriteRenderer sr, Vector2 position, Vector2 targetPosition, Action callback = null, float duration = 1f, float strength = 1f)
	{
		Vector2 normalized = (position - targetPosition).normalized;
		sr.transform.localScale = GetScaleFromMoveDir(normalized.x);
		sr.transform.DOMove(position + normalized * strength, duration * 0.3f).SetEase(Ease.OutExpo).OnComplete(delegate
		{
			sr.transform.DOMove(position, duration * 0.7f).SetEase(Ease.OutBack).OnComplete(delegate
			{
				callback?.Invoke();
			});
		});
	}

	public static void FireRecoil(Transform host, Vector2 dir, Action callback = null, float duration = 0.75f, float distance = 1.2f)
	{
		host.localScale = GetScaleFromMoveDir(dir.x);
		Vector2 sourcePosition = host.position;
		Vector2 vector = sourcePosition + dir * distance;
		host.DOMove(vector, duration * 0.3f).SetEase(Ease.OutExpo).OnComplete(delegate
		{
			host.DOMove(sourcePosition, duration * 0.7f).SetEase(Ease.OutBack).OnComplete(delegate
			{
				callback?.Invoke();
			});
		});
	}

	public static void FireRecoilFront(Transform host, Vector2 dir, Action callback = null, float duration = 0.75f, float distance = 1.2f)
	{
		host.localScale = GetScaleFromMoveDir(dir.x);
		Vector2 vector = (Vector2)host.position + dir * distance;
		host.DOMove(vector, duration).SetEase(Ease.OutExpo).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public static void FireRecoilBack(Transform host, Vector2 src, Action callback = null, float duration = 0.5f)
	{
		host.DOMove(src, duration).SetEase(Ease.OutBack).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public static void FireRecoilFront(SpriteRenderer sr, Vector2 position, Vector2 targetPosition, Action callback = null, float duration = 0.5f, float strength = 1f)
	{
		Vector2 normalized = (position - targetPosition).normalized;
		sr.transform.localScale = GetScaleFromMoveDir(normalized.x);
		sr.transform.DOMove(position + normalized * strength, duration * 0.3f).SetEase(Ease.OutExpo).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}

	public static void FireRecoilBack(SpriteRenderer sr, Vector2 src, Action callback = null, float duration = 0.5f, float strength = 1f)
	{
		sr.transform.DOMove(src, duration * 0.7f).SetEase(Ease.OutBack).OnComplete(delegate
		{
			callback?.Invoke();
		});
	}
}
