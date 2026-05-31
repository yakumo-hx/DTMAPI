using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DolocTown.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class RenderTextureUVHandler : MonoBehaviour
{
	[Serializable]
	private struct CamGeometry
	{
		public static readonly CamGeometry Default = new CamGeometry(5f, 1.7777778f);

		[SerializeField]
		public Vector2 size;

		[SerializeField]
		public Vector2 halfSize;

		[SerializeField]
		public Vector2 sizeReciprocal;

		public CamGeometry(float orthographicSize, float aspectRatio)
		{
			float num = orthographicSize * 2f;
			float num2 = num * aspectRatio;
			size = new Vector2(num2, num);
			halfSize = size * 0.5f;
			sizeReciprocal = new Vector2(1f / num2, 1f / num);
		}
	}

	[SerializeField]
	[HideInInspector]
	private CamGeometry camGeometry = CamGeometry.Default;

	[SerializeField]
	private string uvOffsetPropertyName = string.Empty;

	[SerializeField]
	[HideInInspector]
	private int uvOffsetPropertyId;

	[SerializeField]
	private string uvScalePropertyName = string.Empty;

	[SerializeField]
	[HideInInspector]
	private int uvScalePropertyId;

	[SerializeField]
	private bool shouldDrawDebugBorder = true;

	[SerializeField]
	private Color debugBorderColor = Color.red;

	private SpriteRenderer _spriteRenderer;

	private Camera _cam;

	private SpriteRenderer SpriteRenderer
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

	private Camera mainCam
	{
		get
		{
			if (_cam == null)
			{
				_cam = Camera.main;
			}
			return _cam;
		}
	}

	private Vector2 GeometrySizeRatio
	{
		get
		{
			SpriteRenderer spriteRenderer = SpriteRenderer;
			if (spriteRenderer == null || spriteRenderer.sprite == null)
			{
				return Vector2.zero;
			}
			Vector3 size = spriteRenderer.bounds.size;
			return new Vector2(y: size.y * camGeometry.sizeReciprocal.y, x: size.x * camGeometry.sizeReciprocal.x);
		}
	}

	private Vector2 GeometryPositionRatio
	{
		get
		{
			Vector3 vector = mainCam.transform.position - (Vector3)camGeometry.halfSize;
			return (base.transform.position - vector) * camGeometry.sizeReciprocal;
		}
	}

	private void ResetCamGeometry()
	{
		camGeometry = CalcCamGeometry(mainCam, (float)DolocAPI.worldResolution.x / (float)DolocAPI.worldResolution.y);
	}

	private void OnUvOffsetChanged()
	{
		uvOffsetPropertyId = Shader.PropertyToID(uvOffsetPropertyName);
	}

	private void OnUvScaleChanged()
	{
		uvScalePropertyId = Shader.PropertyToID(uvScalePropertyName);
	}

	private IEnumerable<string> GetMaterialPropertyIDs()
	{
		SpriteRenderer spriteRenderer = SpriteRenderer;
		if (!(spriteRenderer != null))
		{
			return null;
		}
		return ShaderUtils.GetMaterialPropertyIDs(spriteRenderer.sharedMaterial, ShaderPropertyType.Vector);
	}

	public void UpdateUV()
	{
		ResetCamGeometry();
		Vector2 geometrySizeRatio = GeometrySizeRatio;
		Vector2 geometryPositionRatio = GeometryPositionRatio;
		SpriteRenderer.sharedMaterial.SetVector(uvOffsetPropertyId, new Vector4(geometryPositionRatio.x, geometryPositionRatio.y));
		SpriteRenderer.sharedMaterial.SetVector(uvScalePropertyId, new Vector4(geometrySizeRatio.x, geometrySizeRatio.y));
	}

	private void UpdateUVPosition()
	{
		SpriteRenderer.sharedMaterial.SetVector(uvOffsetPropertyId, GeometryPositionRatio);
	}

	public void InitRenderInfo(Vector2 resolution)
	{
		if (resolution.y != 0f)
		{
			camGeometry = CalcCamGeometry(mainCam, resolution.x / resolution.y);
			uvOffsetPropertyId = Shader.PropertyToID(uvOffsetPropertyName);
			uvScalePropertyId = Shader.PropertyToID(uvScalePropertyName);
			Vector2 geometrySizeRatio = GeometrySizeRatio;
			Vector2 geometryPositionRatio = GeometryPositionRatio;
			SpriteRenderer.sharedMaterial.SetVector(uvOffsetPropertyId, new Vector4(geometryPositionRatio.x, geometryPositionRatio.y));
			SpriteRenderer.sharedMaterial.SetVector(uvScalePropertyId, new Vector4(geometrySizeRatio.x, geometrySizeRatio.y));
		}
	}

	private CamGeometry CalcCamGeometry(Camera cam, float screenAspectRatio)
	{
		if (!(cam == null))
		{
			return new CamGeometry(cam.orthographicSize, screenAspectRatio);
		}
		return CamGeometry.Default;
	}

	private void Update()
	{
		UpdateUVPosition();
	}
}
