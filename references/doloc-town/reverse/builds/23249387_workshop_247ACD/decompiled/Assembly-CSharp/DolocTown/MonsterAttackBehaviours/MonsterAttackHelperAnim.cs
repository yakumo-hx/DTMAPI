using System;
using UnityEngine;

namespace DolocTown.MonsterAttackBehaviours;

public class MonsterAttackHelperAnim
{
	private readonly Animator animator;

	private bool shouldWait;

	private float waitDuration;

	private string name;

	private Action callbackOnReady;

	public MonsterAttackHelperAnim(Animator animator)
	{
		this.animator = animator;
	}

	public bool Update(float dt)
	{
		if (shouldWait)
		{
			waitDuration -= dt;
			if (waitDuration <= 0f)
			{
				shouldWait = false;
				callbackOnReady?.Invoke();
				Invoke(name);
			}
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName(name))
		{
			return currentAnimatorStateInfo.normalizedTime >= 1f;
		}
		return true;
	}

	public void Invoke(float readyDuration, string animName, Action readyCallback = null)
	{
		name = animName;
		if (!(readyDuration <= 0f))
		{
			shouldWait = true;
			waitDuration = readyDuration;
			callbackOnReady = readyCallback;
		}
	}

	public void Invoke(string animName)
	{
		animator.Play(animName, 0, 0f);
	}
}
