using RedSaw.Shader;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
[GameEntityManager("/sub_entity/signal_light", DolocGameAssets.GAME_ENTITY_SIGNAL_LIGHT)]
public class SignalLight : GameEntity
{
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	protected SpriteRenderer flareRenderer;

	private MaterialPropertyBlock propertyBlock;

	public float ShineIntensity
	{
		set
		{
			flareRenderer.sharedMaterial.SetFloat("_Intensity", value);
			spriteRenderer.GetPropertyBlock(propertyBlock);
			propertyBlock.SetFloat("_ShineIntensity", value);
			spriteRenderer.SetPropertyBlock(propertyBlock);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		spriteRenderer = GetComponent<SpriteRenderer>();
		propertyBlock = new MaterialPropertyBlock();
	}

	public void Setup(Sprite sprite, Sprite maskSprite)
	{
		if (!(maskSprite == null))
		{
			spriteRenderer.sprite = sprite;
			spriteRenderer.GetPropertyBlock(propertyBlock);
			Vector4 value = maskSprite.CalcMaskSampleUVInfos(sprite);
			propertyBlock.SetVector("_UvCoordinates", value);
			propertyBlock.SetTexture("_EmissionTex", maskSprite.texture);
			propertyBlock.SetFloat("_ShineIntensity", 0f);
			spriteRenderer.SetPropertyBlock(propertyBlock);
		}
	}

	public void Switch(bool value)
	{
		Animator component = GetComponent<Animator>();
		if (value)
		{
			RuntimeAnimatorController asset = DolocAPI.GetAsset<RuntimeAnimatorController>(DolocGameAssets.GAME_ANIM_SIGNAL_LIGHT);
			component.runtimeAnimatorController = asset;
			component.enabled = true;
			component.Play("shine");
		}
		else
		{
			component.enabled = false;
			component.runtimeAnimatorController = null;
			ShineIntensity = 0f;
		}
	}
}
