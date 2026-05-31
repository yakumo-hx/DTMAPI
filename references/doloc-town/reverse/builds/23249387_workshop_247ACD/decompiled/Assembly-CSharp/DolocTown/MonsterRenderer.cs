using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(AttackBehaviourRenderer))]
[RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
public class MonsterRenderer : MonoBehaviour
{
	private Animator animator;

	private SpriteRenderer spriteRenderer;

	private Shiner shiner;

	private Shaker shaker;

	private string currentAnimation;

	private bool shouldCheckAnimationDone;

	private Action onAnimationDone;

	public SpriteRenderer SpriteRenderer => spriteRenderer;

	public Animator Animator => animator;

	protected AttackBehaviourRenderer AttackBehaviourRenderer { get; private set; }

	public void Init()
	{
		animator = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		shiner = new Shiner(spriteRenderer);
		shaker = new Shaker(base.transform);
		AttackBehaviourRenderer = GetComponent<AttackBehaviourRenderer>();
		AttackBehaviourRenderer.Init(this);
	}

	public void PlayAnimation(string name, int layer = 0, float normalizeTime = 0f, Action callback = null)
	{
		if (animator.isActiveAndEnabled && !(animator.runtimeAnimatorController == null))
		{
			animator.Play(name, layer, normalizeTime);
			currentAnimation = name;
			onAnimationDone = callback;
			shouldCheckAnimationDone = callback != null;
		}
	}

	public void OnUpdate(float dt)
	{
		if (shouldCheckAnimationDone)
		{
			AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
			if (currentAnimatorStateInfo.IsName(currentAnimation) && currentAnimatorStateInfo.normalizedTime >= 1f)
			{
				onAnimationDone?.Invoke();
				onAnimationDone = null;
				shouldCheckAnimationDone = false;
			}
		}
	}

	public void OnAttackBegin(MonsterAttackId id, Type type, Transform transform)
	{
		AttackBehaviourRenderer.OnAttackBegin(id, type, transform);
	}

	public void OnAttackEnd(MonsterAttackId id, Type type, Transform transform)
	{
		AttackBehaviourRenderer.OnAttackEnd(id, type, transform);
	}

	public void OnHurt(float duration = 0.1f)
	{
		shiner.Raise(LocMaterials.GAME_MAT_HIT, duration);
		shaker.Shake(duration);
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(spriteRenderer.material);
	}
}
