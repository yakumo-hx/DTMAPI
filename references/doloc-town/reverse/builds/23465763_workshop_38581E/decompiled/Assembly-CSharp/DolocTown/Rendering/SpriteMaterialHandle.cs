using System;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteMaterialHandle : MonoBehaviour
{
	private enum SetTiming
	{
		UnityAwake,
		UnityStart,
		UnityOnEnable
	}

	private enum PropertyType
	{
		Float,
		Color,
		ColorHDR,
		Vector,
		VectorTextureUVInfos,
		VectorTextureOuterUV,
		Texture
	}

	[Serializable]
	private struct MaterialPropertyInfo
	{
		[SerializeField]
		private PropertyType propertyType;

		[SerializeField]
		private string propertyId;

		[SerializeField]
		private float floatValue;

		[SerializeField]
		[ColorUsage(true, false)]
		private Color colorValue;

		[SerializeField]
		[ColorUsage(true, true)]
		private Color colorHDRValue;

		[SerializeField]
		private Vector4 vectorValue;

		[SerializeField]
		private Sprite mainTextureValue;

		[SerializeField]
		private Sprite attachedTextureValue;

		[SerializeField]
		private string attachedTextureId;

		[SerializeField]
		private Sprite textureScaleValue;

		[SerializeField]
		private Texture textureValue;

		private void _Refresh()
		{
			RefreshCurrentShader();
		}

		private void SetMainSprite()
		{
			mainTextureValue = GetMainSprite();
		}

		public void SetProperty(Material material)
		{
			switch (propertyType)
			{
			case PropertyType.Float:
				material.SetFloat(propertyId, floatValue);
				break;
			case PropertyType.Color:
				material.SetColor(propertyId, colorValue);
				break;
			case PropertyType.ColorHDR:
				material.SetColor(propertyId, colorHDRValue);
				break;
			case PropertyType.Texture:
				material.SetTexture(propertyId, textureValue);
				break;
			case PropertyType.Vector:
				material.SetVector(propertyId, vectorValue);
				break;
			case PropertyType.VectorTextureUVInfos:
				if (mainTextureValue != null && attachedTextureValue != null)
				{
					Vector4 outerUV2 = DataUtility.GetOuterUV(mainTextureValue);
					Vector4 outerUV3 = DataUtility.GetOuterUV(attachedTextureValue);
					material.SetVector(propertyId, new Vector4(outerUV2.x, outerUV2.y, outerUV3.x, outerUV3.y));
					material.SetTexture(attachedTextureId, attachedTextureValue.texture);
				}
				break;
			case PropertyType.VectorTextureOuterUV:
				if (textureScaleValue != null)
				{
					Vector4 outerUV = DataUtility.GetOuterUV(textureScaleValue);
					material.SetVector(propertyId, outerUV);
				}
				break;
			}
		}
	}

	private static SpriteMaterialHandle CurrentHandle;

	private static Shader _currentShader;

	[SerializeField]
	private bool isManuallyChoosedShader;

	[SerializeField]
	private Shader _shader;

	[SerializeField]
	private SetTiming setTiming;

	[SerializeField]
	private MaterialPropertyInfo[] _materialPropertyInfos = Array.Empty<MaterialPropertyInfo>();

	private Material _material;

	public static void LoadCurrentShader(SpriteMaterialHandle currentHandle)
	{
		CurrentHandle = currentHandle;
		_currentShader = currentHandle.GetShader();
	}

	public static void RefreshCurrentShader()
	{
		if (!(CurrentHandle == null))
		{
			_currentShader = CurrentHandle.GetShader();
		}
	}

	public static Sprite GetMainSprite()
	{
		if (CurrentHandle == null)
		{
			return null;
		}
		return CurrentHandle.GetComponent<SpriteRenderer>().sprite;
	}

	private void _RefreshShader()
	{
		_currentShader = GetShader();
	}

	public Shader GetShader()
	{
		if (isManuallyChoosedShader)
		{
			return _shader;
		}
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component == null || component.sharedMaterial == null)
		{
			return null;
		}
		_shader = component.sharedMaterial.shader;
		return _shader;
	}

	private void InvokeMaterialStatus()
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component == null)
		{
			Debug.LogWarning("SpriteRenderer of \"" + base.gameObject.scene.name + "." + base.gameObject.name + "\" is not found.");
		}
		else if (!(_shader == null))
		{
			if (_material == null)
			{
				_material = new Material(_shader);
			}
			MaterialPropertyInfo[] materialPropertyInfos = _materialPropertyInfos;
			foreach (MaterialPropertyInfo materialPropertyInfo in materialPropertyInfos)
			{
				materialPropertyInfo.SetProperty(_material);
			}
			component.sharedMaterial = _material;
		}
	}

	private void Awake()
	{
		if (setTiming == SetTiming.UnityAwake)
		{
			InvokeMaterialStatus();
		}
	}

	private void Start()
	{
		if (setTiming == SetTiming.UnityStart)
		{
			InvokeMaterialStatus();
		}
	}

	private void OnEnable()
	{
		if (setTiming == SetTiming.UnityOnEnable)
		{
			InvokeMaterialStatus();
		}
	}

	public void OnDestroy()
	{
		if (_material != null)
		{
			UnityEngine.Object.Destroy(_material);
		}
	}
}
