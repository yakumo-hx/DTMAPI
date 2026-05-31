using System;
using System.Collections.Generic;
using RedSaw.Shader;
using UnityEngine;
using UnityEngine.Rendering;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteUVCorrector : MonoBehaviour
{
	[SerializeField]
	private bool correctOnStart = true;

	[SerializeField]
	[Tooltip("遮罩纹理默认与主纹理规格一致")]
	private Sprite mask;

	[SerializeField]
	private string propertyNameMask;

	[SerializeField]
	private string propertyNameUV;

	private string[] _availableUVProperties;

	private string[] _availableMaskProperties;

	private MaterialPropertyBlock propertyBlock;

	private string[] AvailableUVProperties
	{
		get
		{
			if (_availableUVProperties == null)
			{
				_availableUVProperties = GetPropertyNames(ShaderPropertyType.Vector);
			}
			return _availableUVProperties;
		}
	}

	private string[] AvailableMaskProperties
	{
		get
		{
			if (_availableMaskProperties == null)
			{
				_availableMaskProperties = GetPropertyNames(ShaderPropertyType.Texture);
			}
			return _availableMaskProperties;
		}
	}

	private string[] UpdateUVProperties()
	{
		_availableUVProperties = GetPropertyNames(ShaderPropertyType.Vector);
		return _availableUVProperties;
	}

	private string[] UpdateMaskProperties()
	{
		_availableMaskProperties = GetPropertyNames(ShaderPropertyType.Texture);
		return _availableMaskProperties;
	}

	private string[] GetPropertyNames(ShaderPropertyType propertyType)
	{
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component == null || component.sprite == null)
		{
			return Array.Empty<string>();
		}
		Shader shader = component.sharedMaterial.shader;
		if (shader == null)
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		for (int i = 0; i < shader.GetPropertyCount(); i++)
		{
			if (shader.GetPropertyType(i) == propertyType)
			{
				list.Add(shader.GetPropertyName(i));
			}
		}
		return list.ToArray();
	}

	private void Start()
	{
		if (correctOnStart && DolocAPI.IsGameInitialized)
		{
			Correct();
		}
	}

	public void Correct()
	{
		if (propertyBlock == null)
		{
			propertyBlock = new MaterialPropertyBlock();
		}
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (!(component == null) && !(component.sprite == null) && !string.IsNullOrEmpty(propertyNameUV))
		{
			Vector4 value = mask.CalcMaskSampleUVInfos(component.sprite);
			component.GetPropertyBlock(propertyBlock);
			propertyBlock.SetVector(propertyNameUV, value);
			propertyBlock.SetTexture(propertyNameMask, mask.texture);
			component.SetPropertyBlock(propertyBlock);
		}
	}
}
