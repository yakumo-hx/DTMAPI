using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
[GameEntityManager("/sub_entity/grid_renderer/objects", DolocGameAssets.GAME_ENTITY_GRID_OBJECT, Threshold = 30, Frequency = 50)]
public class GridRenderer : GameEntity
{
	private SpriteRenderer sr;

	private Material _material;

	private Vector2Int _gridSize;

	private Vector2Int _gridPosition;

	public Vector2Int GridSize
	{
		get
		{
			return _gridSize;
		}
		set
		{
			if (!(_material == null))
			{
				_gridSize = value;
				base.transform.localScale = new Vector3(value.x, value.y, 1f);
				_material.SetVector("_GridSize", new Vector4(value.x, value.y, 0f, 0f));
			}
		}
	}

	public Vector2Int GridPosition
	{
		get
		{
			return _gridPosition;
		}
		set
		{
			_gridPosition = value;
			base.transform.position = new Vector3((float)value.x * 1.5f, (float)value.y * 1.5f, 0f);
		}
	}

	public float BorderDistance
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetFloat("_BorderDistance", Mathf.Clamp01(value));
			}
		}
	}

	public Color BorderColor1
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetColor("_BorderColor", value);
			}
		}
	}

	public Color BorderColor2
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetColor("_BorderColor2", value);
			}
		}
	}

	public float BorderThickness
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetFloat("_BorderThickness", value);
			}
		}
	}

	public float BorderOffsetSpeed
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetFloat("_BorderSpeed", value);
			}
		}
	}

	public float GridLineThickness
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetFloat("_GridThickness", value);
			}
		}
	}

	public Color CoverColor
	{
		set
		{
			if (!(_material == null))
			{
				_material.SetColor("_CoverColor", value);
			}
		}
	}

	protected override void __Init()
	{
		base.__Init();
		sr = GetComponent<SpriteRenderer>();
		if (sr != null && sr.sharedMaterial != null)
		{
			_material = new Material(sr.sharedMaterial);
			sr.material = _material;
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		BorderThickness = 0.17f;
		GridLineThickness = 0.15f;
		BorderOffsetSpeed = 6f;
		CoverColor = new Color(0.7428691f, 0.7087932f, 0.8301887f, 0.3411765f);
	}

	private void OnDestroy()
	{
		if (_material != null)
		{
			Object.Destroy(_material);
		}
	}
}
