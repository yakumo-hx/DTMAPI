using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
[GameEntityManager("/farm/crop_tree", DolocGameAssets.GAME_ENTITY_CROP_TREE)]
public class TreeCropRenderer : GameEntity, IFellable
{
	protected PolygonCollider2D col;

	private bool isShine;

	private Tween anim;

	public SpriteRenderer sp { get; protected set; }

	public TreeCrop crop { get; set; }

	public Sprite sprite
	{
		get
		{
			return sp.sprite;
		}
		set
		{
			sp.sprite = value;
			col.SetPath(0, SpriteShapeBuffer._GetSpriteShape(value));
		}
	}

	public bool ShouldCostEnergy => true;

	public bool ShouldCostChopCounter => false;

	public void UpdateColliderPath()
	{
		col.SetPath(0, SpriteShapeBuffer._GetSpriteShape(sp.sprite));
	}

	protected override void __Init()
	{
		base.__Init();
		sp = GetComponent<SpriteRenderer>();
		col = GetComponent<PolygonCollider2D>();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (crop != null)
		{
			crop.Renderer = null;
			crop = null;
		}
	}

	public void Shine(float duration = 0.3f)
	{
	}

	public void Shake(float duration = 0.3f, float shakeStrength = 0.1f)
	{
		anim?.Kill();
		anim = base.transform.DOShakePosition(duration, new Vector3(shakeStrength, 0f, 0f));
	}

	private void OnDestroy()
	{
		Object.Destroy(sp.material);
	}

	public bool OnFell(ItemTool tool, Vector2 hitPosition)
	{
		return crop?.OnFell(tool, hitPosition) ?? false;
	}
}
