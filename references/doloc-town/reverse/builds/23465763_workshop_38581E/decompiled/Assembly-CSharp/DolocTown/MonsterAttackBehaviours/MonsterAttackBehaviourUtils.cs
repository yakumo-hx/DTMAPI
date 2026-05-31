using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public static class MonsterAttackBehaviourUtils
{
	public static Tween DoImpact(Transform transform, Vector2 dashDir, float dashDistance, float dashDuration, Ease dashEase, Action updateAction = null, Action endAction = null)
	{
		Vector2 vector = (Vector2)transform.position + dashDir * dashDistance;
		transform.localScale = MonsterUtils.GetScaleFromShootDir(dashDir.x);
		return transform.DOMove(vector, dashDuration).OnUpdate(delegate
		{
			updateAction?.Invoke();
		}).SetEase(dashEase);
	}
}
