using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public static class LightPatch
{
	private static readonly int MainTex = Shader.PropertyToID("_MainTex");

	private static readonly int EmissionTex = Shader.PropertyToID("_EmissionTex");

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private static readonly int EmissionIntensity = Shader.PropertyToID("_EmissionIntensity");

	private static readonly int EmissionTexPosition = Shader.PropertyToID("_EmissionTex_Position");

	private static readonly int ToggleShake = Shader.PropertyToID("_ToggleShake");

	public static void ToggleLightOff(this SpriteRenderer renderer)
	{
		renderer.sharedMaterial = LocMaterials.GAME_MAT_2D;
	}

	public static void ToggleLightOn(this SpriteRenderer renderer, Sprite emissionTexture, Color emissionColor, float emissionIntensity = 1f, bool toggleShake = false)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		renderer.sharedMaterial = DolocAPI.GetAsset<Material>(DolocGameAssets.GAME_MAT_SPEC_LIGHT);
		renderer.GetPropertyBlock(materialPropertyBlock);
		Vector4 outerUV = DataUtility.GetOuterUV(renderer.sprite);
		Vector4 outerUV2 = DataUtility.GetOuterUV(emissionTexture);
		Vector4 value = new Vector4(outerUV2.x, outerUV2.y, outerUV.x, outerUV.y);
		materialPropertyBlock.SetTexture(MainTex, renderer.sprite.texture);
		materialPropertyBlock.SetTexture(EmissionTex, emissionTexture.texture);
		materialPropertyBlock.SetColor(EmissionColor, emissionColor);
		materialPropertyBlock.SetFloat(EmissionIntensity, emissionIntensity);
		materialPropertyBlock.SetVector(EmissionTexPosition, value);
		materialPropertyBlock.SetFloat(ToggleShake, toggleShake ? 1f : 0f);
		renderer.SetPropertyBlock(materialPropertyBlock);
	}
}
