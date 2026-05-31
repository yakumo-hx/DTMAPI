using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[GameEntityManager("/sub_entity/grid_renderer/area_interacted", DolocGameAssets.GAME_ENTITY_GRID_AREA_INTERACTED)]
[RequireComponent(typeof(SpriteRenderer))]
public class GridArea : GameEntity
{
	private SpriteRenderer sr;

	private Material _material;

	private Vector2Int _gridPosition;

	private float _originAlpha;

	private float _alpha;

	private Sequence shinySequence;

	public Vector2Int GridSize
	{
		set
		{
			if (!(_material == null) && value.x * value.y != 0)
			{
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

	public float Alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			_alpha = value;
			_material.SetFloat("_GlobalAlpha", _originAlpha * _alpha);
		}
	}

	public Color Color
	{
		set
		{
			_material.SetColor("_Color", value);
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
		_originAlpha = _material.GetFloat("_GlobalAlpha");
	}

	private void OnDestroy()
	{
		if (_material != null)
		{
			UnityEngine.Object.Destroy(_material);
		}
		shinySequence?.Kill();
	}

	public void ShineArea(Color color, Action callback = null, float alpha = 1f, float fadeInDuration = 0.6f, float lastDuration = 0.6f, float fadeOutDuration = 4f)
	{
		Alpha = 0f;
		Color = color;
		shinySequence?.Kill();
		shinySequence = DOTween.Sequence();
		shinySequence.Append(DOTween.To(() => Alpha, delegate(float x)
		{
			Alpha = x;
		}, alpha, fadeInDuration));
		shinySequence.Append(DOTween.To(() => Alpha, delegate(float x)
		{
			Alpha = x;
		}, alpha, lastDuration));
		shinySequence.Append(DOTween.To(() => Alpha, delegate(float x)
		{
			Alpha = x;
		}, 0f, fadeOutDuration));
		shinySequence.OnComplete(delegate
		{
			callback?.Invoke();
		});
	}
}
