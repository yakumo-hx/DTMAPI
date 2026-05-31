using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
[GameEntityManager("/sub_entity/", DolocGameAssets.GAME_ENTITY_ANIMATOR)]
public class GameEntityAnimator : GameEntity
{
	public const string ANIM_STARS = "stars";

	public Animator Animator { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		Animator = GetComponent<Animator>();
	}

	public void Play(string anim, float normalizedTime = 0f)
	{
		if (!base.isVisible)
		{
			SetVisible(value: true);
		}
		Animator.Play(anim, -1, normalizedTime);
	}

	public void Play(string anim, Vector2 position, float normalizedTime = 0f)
	{
		base.transform.position = position;
		Play(anim, normalizedTime);
	}
}
