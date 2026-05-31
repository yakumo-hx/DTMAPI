using UnityEngine;
using UnityEngine.Rendering;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundLayerRenderer : DolocRecyclableObject
{
	private Material _material;

	private float _offsetAccumulation;

	private static readonly int OffsetX = Shader.PropertyToID("_OffsetX");

	private static readonly int Saturation = Shader.PropertyToID("_Saturation");

	private static readonly int Lightness = Shader.PropertyToID("_Lightness");

	private SpriteRenderer _spriteRenderer;

	public SpriteRenderer SpriteRenderer
	{
		get
		{
			if (_spriteRenderer == null)
			{
				_spriteRenderer = GetComponent<SpriteRenderer>();
			}
			return _spriteRenderer;
		}
	}

	public void LoadLayer(Sprite sprite, Shader shader, float saturation, float lightness, string sortingLayer, Color color, int orderInLayer, bool hasAlpha = false, float alpha = 1f)
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		component.sprite = sprite;
		component.sortingLayerName = sortingLayer;
		component.sortingOrder = orderInLayer;
		component.color = (hasAlpha ? color.Alpha(alpha) : color);
		if (_material == null)
		{
			_material = new Material(shader);
			component.material = _material;
		}
		_material.SetFloat(Saturation, saturation);
		_material.SetFloat(Lightness, lightness);
	}

	public void SetMaskable(bool value)
	{
		CompareFunction compareFunction = (value ? CompareFunction.Equal : CompareFunction.Disabled);
		_material.SetFloat("_StencilComp", (float)compareFunction);
	}

	public void UpdatePosition(float speedX, float accumulationOffset, float fixedOffset, float speedY)
	{
		_offsetAccumulation += accumulationOffset;
		_material.SetFloat(OffsetX, base.transform.position.x * speedX + fixedOffset + _offsetAccumulation);
		Transform obj = base.transform;
		Vector3 localPosition = obj.localPosition;
		localPosition.y = speedY;
		obj.localPosition = localPosition;
	}

	private void OnDestroy()
	{
		Object.Destroy(_material);
	}

	private void OnDisable()
	{
		_offsetAccumulation = 0f;
	}
}
