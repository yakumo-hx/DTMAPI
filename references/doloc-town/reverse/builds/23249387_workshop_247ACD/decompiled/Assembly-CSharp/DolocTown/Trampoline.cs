using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DolocTown;

public class Trampoline : InteractableObjectExclude
{
	private Animator animator;

	protected override ITouchCheckStrategy touchChecker { get; set; }

	private TrampolineTouchChecker checker => touchChecker as TrampolineTouchChecker;

	protected override void __Init()
	{
		base.__Init();
		touchChecker = new TrampolineTouchChecker();
		animator = GetComponent<Animator>();
	}

	protected override void OnTouch()
	{
		base.OnTouch();
		if (checker.isDropDown)
		{
			animator.Play("bounce", 0, 0f);
			WaitToJump().Forget();
		}
	}

	private async UniTaskVoid WaitToJump()
	{
		await UniTask.WaitUntil(() => !base.isTouched || DolocAPI.agent.StateManager.current.SupportJump);
		if (base.isTouched)
		{
			DolocAPI.agent.SuperJump();
			base.isTouched = false;
		}
	}
}
