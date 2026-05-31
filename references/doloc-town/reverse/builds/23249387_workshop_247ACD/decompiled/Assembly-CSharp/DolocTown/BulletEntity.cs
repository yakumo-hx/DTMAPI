using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BulletEntity : GameEntity
{
	private SpriteRenderer spriteRenderer;

	private Animator animator;

	private BoxCollider2D boxCollider;

	private CircleCollider2D circleCollider;

	private BulletColliderWithGround groundCollider;

	public Action<Collider2D> callbackOnHit { get; set; }

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		animator = GetComponent<Animator>();
		boxCollider = GetComponent<BoxCollider2D>();
		circleCollider = GetComponent<CircleCollider2D>();
		groundCollider = GetComponentInChildren<BulletColliderWithGround>();
		groundCollider.Init();
	}

	public bool IsCollidingWithWall()
	{
		return groundCollider.IsCollidingWithWall();
	}

	public void Configure(string animation, Vector2 size, Vector2 offset)
	{
		animator.Play(animation, 0, 0f);
		boxCollider.size = size;
		boxCollider.offset = offset;
		boxCollider.enabled = true;
		circleCollider.enabled = false;
	}

	public void Configure(string animation, float radius, Vector2 offset)
	{
		animator.Play(animation, 0, 0f);
		circleCollider.radius = radius;
		circleCollider.offset = offset;
		circleCollider.enabled = true;
		boxCollider.enabled = false;
	}

	public void OnTriggerEnter2D(Collider2D other)
	{
		if (!BattleUtils.IsWall(other))
		{
			callbackOnHit?.Invoke(other);
		}
	}
}
