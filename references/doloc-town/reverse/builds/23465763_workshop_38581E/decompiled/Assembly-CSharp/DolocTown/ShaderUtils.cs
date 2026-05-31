using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public static class ShaderUtils
{
	public static void RenderAsNormal(this SpriteRenderer renderer)
	{
		renderer.sharedMaterial = LocMaterials.GAME_MAT_2D;
		renderer.color = Color.white;
	}

	public static bool Toggle2DEmission(this SpriteRenderer renderer, Sprite mainSprite, Sprite maskSprite, Color color)
	{
		if (renderer == null || mainSprite == null || maskSprite == null)
		{
			return false;
		}
		renderer.sharedMaterial = DolocGameAssets.GAME_MAT_2D_EMISSION.LoadMaterial();
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		renderer.GetPropertyBlock(materialPropertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(mainSprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(maskSprite);
		Vector4 value = new Vector4(outerUV2.x, outerUV2.y, outerUV.x, outerUV.y);
		materialPropertyBlock.SetTexture("_EmissionTex", maskSprite.texture);
		materialPropertyBlock.SetVector("_UvCoordinates", value);
		materialPropertyBlock.SetColor("_EmissionColor", color);
		renderer.SetPropertyBlock(materialPropertyBlock);
		return true;
	}

	public static bool ToggleItemShine(this SpriteRenderer renderer, Material material)
	{
		if (renderer == null || renderer.sprite == null)
		{
			return false;
		}
		renderer.sharedMaterial = material;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		renderer.GetPropertyBlock(materialPropertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(renderer.sprite);
		materialPropertyBlock.SetVector(ShaderConst.MainTexUVInfos, outerUV);
		renderer.SetPropertyBlock(materialPropertyBlock);
		return true;
	}

	public static bool ToggleItemShine(this SpriteRenderer renderer)
	{
		return renderer.ToggleItemShine(LocMaterials.GAME_MAT_2D_LIGHTSWEEP);
	}

	public static bool ToggleAlphaMask(this SpriteRenderer renderer, Sprite alphaMask, bool shouldRevert = false)
	{
		if (alphaMask == null)
		{
			return false;
		}
		int num = (shouldRevert ? 1 : 0);
		renderer.sharedMaterial = LocMaterials.GAME_MAT_2D_ALPHAMASK;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		renderer.GetPropertyBlock(materialPropertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(renderer.sprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(alphaMask);
		Vector4 value = new Vector4(outerUV.x, outerUV.y, outerUV2.x, outerUV2.y);
		materialPropertyBlock.SetTexture(ShaderConst.AlphaMask, alphaMask.texture);
		materialPropertyBlock.SetVector(ShaderConst.MainTexUVInfos, value);
		materialPropertyBlock.SetFloat("_Intensity", num);
		renderer.SetPropertyBlock(materialPropertyBlock);
		return true;
	}

	public static void RenderAsFishTank(this SpriteRenderer renderer, Sprite maskSprite, int fishCount, int index, Material givenMaterial = null)
	{
		Sprite sprite = renderer.sprite;
		if (!(sprite == null) && !(maskSprite == null))
		{
			if (givenMaterial == null)
			{
				givenMaterial = LocMaterials.GAME_MAT_FISH_TANK;
			}
			renderer.sharedMaterial = givenMaterial;
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			Vector4 outerUV = DataUtility.GetOuterUV(sprite);
			Vector4 outerUV2 = DataUtility.GetOuterUV(maskSprite);
			fishCount = Mathf.Clamp(fishCount, 0, 10);
			renderer.sprite = sprite;
			renderer.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetTexture(ShaderConst.AlphaMask, maskSprite.texture);
			materialPropertyBlock.SetInt("_FishCount", fishCount);
			materialPropertyBlock.SetVector(ShaderConst.MainTexUVInfos, outerUV);
			materialPropertyBlock.SetVector("_AlphaMask_UVInfos", outerUV2);
			materialPropertyBlock.SetFloat("_FishShadowOffset", index);
			renderer.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public static void RenderAsFishIncubator(this SpriteRenderer renderer, Sprite maskSprite, Sprite emissionSprite, bool showLight)
	{
		Sprite sprite = renderer.sprite;
		if (!(sprite == null) && !(maskSprite == null))
		{
			renderer.sharedMaterial = LocMaterials.GAME_MAT_FISH_INCUBATOR;
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			renderer.sprite = sprite;
			renderer.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetTexture(ShaderConst.AlphaMask, maskSprite.texture);
			materialPropertyBlock.SetVector(ShaderConst.MainTexUVInfos, DataUtility.GetOuterUV(sprite));
			materialPropertyBlock.SetVector("_AlphaMask_UVInfos", DataUtility.GetOuterUV(maskSprite));
			materialPropertyBlock.SetTexture("_EmissionTex", emissionSprite.texture);
			materialPropertyBlock.SetVector("_EmissionTex_UVInfos", DataUtility.GetOuterUV(emissionSprite));
			materialPropertyBlock.SetFloat("_EnableEmission", showLight ? 1 : 0);
			materialPropertyBlock.SetColor("_EmissionColor", LocMaterials.GAME_MAT_FISH_INCUBATOR.GetColor("_EmissionColor"));
			renderer.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public static void SetFishTankFishCount(this SpriteRenderer R, int value)
	{
		int value2 = Mathf.Clamp(value, 0, 10);
		if (R != null)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			R.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetInt("_FishCount", value2);
			R.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public static void RenderAsCropTrafficLight(this SpriteRenderer R, Sprite maskSprite)
	{
		R.sharedMaterial = LocMaterials.GAME_MAT_CROP_TRAFFIC_LIGHT;
		if (maskSprite == null)
		{
			Debug.LogError("SpriteRenderer.RenderAsCropTrafficLight: 未设置遮罩贴图");
			return;
		}
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		Vector4 outerUV = DataUtility.GetOuterUV(R.sprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(maskSprite);
		R.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetVector(ShaderConst.MainTexUVInfos, outerUV);
		materialPropertyBlock.SetVector(ShaderConst.MaskTextUVInfos, outerUV2);
		materialPropertyBlock.SetTexture("_MaskTex", maskSprite.texture);
		Material gAME_MAT_CROP_TRAFFIC_LIGHT = LocMaterials.GAME_MAT_CROP_TRAFFIC_LIGHT;
		materialPropertyBlock.SetColor("_ColorA", gAME_MAT_CROP_TRAFFIC_LIGHT.GetColor("_ColorA"));
		materialPropertyBlock.SetColor("_ColorB", gAME_MAT_CROP_TRAFFIC_LIGHT.GetColor("_ColorB"));
		R.SetPropertyBlock(materialPropertyBlock);
	}

	public static void UpdateCropTrafficLight(this SpriteRenderer R, bool isLeftLightOn, bool isRightLightOn)
	{
		if (!(R == null) && !(R.sharedMaterial != LocMaterials.GAME_MAT_CROP_TRAFFIC_LIGHT))
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			R.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetFloat("_ColorAIntensity", isLeftLightOn ? 1 : 0);
			materialPropertyBlock.SetFloat("_ColorBIntensity", isRightLightOn ? 1 : 0);
			R.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public static void RenderAsLantern(this SpriteRenderer R, Sprite emissionSprite)
	{
		Sprite sprite = R.sprite;
		if (!(sprite == null) && !(emissionSprite == null))
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			R.sprite = sprite;
			R.GetPropertyBlock(materialPropertyBlock);
			materialPropertyBlock.SetVector(ShaderConst.MainTexUVInfos, DataUtility.GetOuterUV(sprite));
			materialPropertyBlock.SetVector(ShaderConst.MaskTextUVInfos, DataUtility.GetOuterUV(emissionSprite));
			materialPropertyBlock.SetTexture("_MaskTex", emissionSprite.texture);
			R.SetPropertyBlock(materialPropertyBlock);
		}
	}

	public static void RenderAsNormalCrop(this SpriteRenderer R)
	{
		R.sharedMaterial = DolocGameAssets.GAME_MAT_CROP.LoadMaterial();
		R.color = Color.white.Alpha(0f);
	}

	public static void RenderAsFirefly(this SpriteRenderer R, MaterialPropertyBlock propertyBlock, Sprite sprite)
	{
		R.sharedMaterial = DolocGameAssets.GAME_MAT_RIM_LIGHT.LoadMaterial();
		R.GetPropertyBlock(propertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(sprite);
		propertyBlock.SetVector("_MainTex_UVInfos", outerUV);
		R.SetPropertyBlock(propertyBlock);
		R.color = R.color.Alpha(0f);
	}
}
