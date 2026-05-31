using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[GameEffectsManager("/anims", DolocGameAssets.GAME_ENTITY_INSTANIM)]
public class InstantAnimationEffects : GameEntity
{
	private Tween _tween;

	public Action<InstantAnimationEffects> Recycle { get; set; }

	public Animator animator { get; private set; }

	public SpriteRenderer sp { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		animator = GetComponent<Animator>();
		sp = GetComponent<SpriteRenderer>();
	}

	public void Play(Vector2 position, string name)
	{
		base.transform.position = position;
		animator.Play(name, 0, 0f);
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		_tween?.Kill();
		_tween = null;
	}

	private void OnAnimationDone()
	{
		Recycle(this);
	}

	private void __AnimationKeyEvent_YawnMove()
	{
		_tween = base.transform.DOMove(position + new Vector3(base.transform.localScale.x, 0.5f) * 1.5f, 1f);
	}

	private void _SetFlashScreen()
	{
		DolocAPI.SetPPEnabled(PPTypes.FLASH, value: true);
	}

	private void _UnsetFlashScreen()
	{
		DolocAPI.SetPPEnabled(PPTypes.FLASH, value: false);
	}
}
