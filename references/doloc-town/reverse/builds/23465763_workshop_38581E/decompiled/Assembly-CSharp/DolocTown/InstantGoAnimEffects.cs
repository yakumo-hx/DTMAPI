using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(Animator))]
public class InstantGoAnimEffects : InstantGoEffects
{
	private Animator _animator;

	protected override void __Init()
	{
		base.__Init();
		_animator = GetComponent<Animator>();
	}

	public override void Raise()
	{
		Play();
	}

	private void Play()
	{
		if (!(_animator.runtimeAnimatorController == null))
		{
			RuntimeAnimatorController runtimeAnimatorController = _animator.runtimeAnimatorController;
			if (runtimeAnimatorController.animationClips.Length != 0)
			{
				_animator.Play(runtimeAnimatorController.animationClips[0].name, 0, 0f);
			}
		}
	}

	public void OnAnimationDone()
	{
		base.Recycle?.Invoke(this);
	}
}
