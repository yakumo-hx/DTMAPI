using JetBrains.Annotations;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/sub_entity/single_sprite", DolocGameAssets.GAME_ENTITY_EQUIPMENT_SINGLESPRITE)]
public class SingleSpriteRender : GameEntity
{
	public SpriteRenderer SpriteRenderer { get; private set; }

	public Material material
	{
		get
		{
			return SpriteRenderer.sharedMaterial;
		}
		set
		{
			SpriteRenderer.sharedMaterial = value;
		}
	}

	[NotNull]
	public Sprite sprite
	{
		get
		{
			return SpriteRenderer.sprite;
		}
		set
		{
			if (value == null)
			{
				SpriteRenderer.color = DolocColor.empty;
				return;
			}
			SpriteRenderer.color = DolocColor.white;
			SpriteRenderer.sprite = value;
		}
	}

	public float Alpha
	{
		get
		{
			return SpriteRenderer.color.a;
		}
		set
		{
			Color color = SpriteRenderer.color;
			color.a = value;
			SpriteRenderer.color = color;
		}
	}

	public Color Color
	{
		get
		{
			return SpriteRenderer.color;
		}
		set
		{
			SpriteRenderer.color = value;
		}
	}

	public string SortingLayerName
	{
		get
		{
			return SpriteRenderer.sortingLayerName;
		}
		set
		{
			SpriteRenderer.sortingLayerName = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		SpriteRenderer = GetComponent<SpriteRenderer>();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		Reset();
	}

	public override void OnReuse()
	{
		base.OnReuse();
		Reset();
	}

	public void Reset()
	{
		material = LocMaterials.GAME_MAT_2D;
		Color = DolocColor.white;
		SortingLayerName = "Default";
		SpriteRenderer.sortingOrder = 0;
		base.transform.rotation = Quaternion.identity;
		base.transform.localRotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
	}
}
