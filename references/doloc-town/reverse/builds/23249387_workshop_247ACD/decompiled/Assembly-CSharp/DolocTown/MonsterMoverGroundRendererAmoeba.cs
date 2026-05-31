using UnityEngine;

namespace DolocTown;

public class MonsterMoverGroundRendererAmoeba : MonsterMoverGroundRenderer
{
	private Animator _animator;

	private Animator Animator
	{
		get
		{
			if (_animator == null)
			{
				_animator = GetComponent<Animator>();
			}
			return _animator;
		}
	}

	private bool IsAnimationDone(string name)
	{
		AnimatorStateInfo currentAnimatorStateInfo = Animator.GetCurrentAnimatorStateInfo(0);
		if (currentAnimatorStateInfo.IsName(name))
		{
			return currentAnimatorStateInfo.normalizedTime >= 1f;
		}
		return false;
	}

	public override bool IsJumpReadyDone()
	{
		return IsAnimationDone("jump_ready");
	}

	public override bool IsTouchGroundDone()
	{
		return IsAnimationDone("touch_ground");
	}

	public override void OnMove()
	{
		Animator.Play("move");
	}

	public override void OnReadyJump()
	{
		Animator.Play("jump_ready", 0, 0f);
	}

	public override void OnJump()
	{
		Animator.Play("jump", 0, 0f);
	}

	public override void OnDrop()
	{
		Animator.Play("drop", 0, 0f);
	}

	public override void OnTouchGround()
	{
		Animator.Play("touch_ground", 0, 0f);
	}
}
