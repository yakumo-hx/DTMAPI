using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/sub_entity/grid_background", DolocGameAssets.GAME_ENTITY_GRID_BACKGROUND)]
public class GridBackground : GameEntity
{
	private SpriteRenderer sr;

	public Color GridColor
	{
		get
		{
			return sr.color;
		}
		set
		{
			sr.color = value;
		}
	}

	public Vector2Int GridSize
	{
		get
		{
			return new Vector2Int((int)base.transform.localScale.x, (int)base.transform.localScale.y);
		}
		set
		{
			if (!(sr == null) && !(sr.sharedMaterial == null) && value.x * value.y != 0)
			{
				base.transform.localScale = new Vector3(value.x, value.y, 1f);
				sr.sharedMaterial.SetVector("_GridSize", new Vector4(value.x, value.y, 0f, 0f));
			}
		}
	}

	public Vector2Int GridPosition
	{
		set
		{
			base.transform.position = new Vector3((float)value.x * 1.5f, (float)value.y * 1.5f, 0f);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		sr = GetComponent<SpriteRenderer>();
	}
}
