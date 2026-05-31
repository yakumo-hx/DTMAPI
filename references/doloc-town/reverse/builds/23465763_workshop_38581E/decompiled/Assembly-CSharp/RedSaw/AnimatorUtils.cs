using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using UnityEngine;

namespace RedSaw;

public static class AnimatorUtils
{
	public static bool IsAnimationDone(this Animator animator, string name)
	{
		if (animator == null || name.IsNullOrEmpty())
		{
			return false;
		}
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName(name))
		{
			return currentAnimatorStateInfo.normalizedTime >= 1f;
		}
		return false;
	}

	public static IEnumerable<string> GetAnimationNames(this RuntimeAnimatorController controller)
	{
		if (controller == null)
		{
			return Array.Empty<string>();
		}
		return controller.animationClips.Select((AnimationClip x) => x.name).Distinct();
	}
}
