using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public abstract class WorldContentRenderer : GameEntity, IInteractable
{
	protected SpriteRenderer Sr;

	protected CircleCollider2D Col2d;

	public WorldContent WorldContent { get; set; }

	public abstract bool OnlyTouch { get; }

	public virtual bool CanInteractContinues => false;

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
		Col2d = GetComponent<CircleCollider2D>();
	}

	public virtual void SetSprite(Sprite itemSprite)
	{
		Sr.sprite = itemSprite;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (WorldContent != null)
		{
			WorldContent.BaseRenderer = null;
			WorldContent = null;
		}
	}

	public virtual void OnTouch()
	{
		WorldContent?.OnTouch();
	}

	public virtual void OnDisTouch()
	{
		WorldContent?.OnDisTouch();
	}

	public virtual void OnInteract()
	{
		WorldContent?.OnInteract();
	}
}
