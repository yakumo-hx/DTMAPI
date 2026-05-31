using DG.Tweening;
using DolocTown.Config.Equipment;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class LampController : LampControllerBase
{
	private readonly Equipment equipment;

	public bool toggleOn { get; private set; }

	private EquipmentRenderer Renderer => equipment.Renderer;

	protected override Sprite EmissionSprite
	{
		get
		{
			if (equipment.Turn && lampInfo.EmissionRevertSpriteAsset.Asset != null)
			{
				return lampInfo.EmissionRevertSpriteAsset.Asset;
			}
			return lampInfo.EmissionSpriteAsset.Asset;
		}
	}

	public LampController(Equipment equipment, LampInfo lampInfo)
		: base(lampInfo)
	{
		this.equipment = equipment;
	}

	private float GetIntensityFactor()
	{
		return equipment.Host.GetLightIntensityFactor(equipment);
	}

	public override void ToggleLight(bool value)
	{
		if (!(Renderer == null))
		{
			toggleOn = value;
			equipment.Host.AdjustLightIntensityFlag = true;
			DolocAPI.RefreshSunLight(0f);
			ToggleMat(value);
			if (value)
			{
				LampRenderer lampRenderer = Renderer.GetLampRenderer();
				lampRenderer.Render(lampInfo, Renderer.position2d);
				lampRenderer.IntensityScale = GetIntensityFactor();
			}
			else
			{
				Renderer.RemoveRenderComponent<LampRenderer>();
			}
		}
	}

	public override void ToggleLight(bool value, float time)
	{
		LampRenderer lampRenderer;
		if (!(Renderer == null))
		{
			toggleOn = value;
			equipment.Host.AdjustLightIntensityFlag = true;
			DolocAPI.RefreshSunLight(value ? time : 0f);
			ToggleMat(value);
			lampRenderer = Renderer.GetLampRenderer();
			if (value)
			{
				lampRenderer.Render(lampInfo, Renderer.position2d);
				fadeFunc(0f);
				noiseTween?.Kill();
				noiseTween = DOTween.To(fadeFunc, 0f, GetIntensityFactor(), time);
			}
			else
			{
				lampRenderer.SetVisible(value: false);
				lampRenderer.Color = Color.white;
				Renderer.RemoveRenderComponent<LampRenderer>();
			}
		}
		void fadeFunc(float v)
		{
			lampRenderer.IntensityScale = v;
			Color color = lampInfo.Color;
			color.a = v;
			lampRenderer.Color = color;
		}
	}

	protected override void ToggleMat(bool value)
	{
		toggleOn = value;
		if (value)
		{
			Renderer.Sr.sharedMaterial = DolocAPI.GetAsset<Material>(DolocGameAssets.GAME_MAT_SPEC_LIGHT);
			Renderer.Sr.GetPropertyBlock(propertyBlock);
			Vector4 outerUV = DataUtility.GetOuterUV(Renderer.Sr.sprite);
			Vector4 outerUV2 = DataUtility.GetOuterUV(EmissionSprite);
			Vector4 value2 = new Vector4(outerUV2.x, outerUV2.y, outerUV.x, outerUV.y);
			propertyBlock.SetTexture("_MainTex", Renderer.Sr.sprite.texture);
			propertyBlock.SetTexture("_EmissionTex", EmissionSprite.texture);
			propertyBlock.SetColor("_EmissionColor", lampInfo.EmissionColor);
			propertyBlock.SetFloat("_EmissionIntensity", lampInfo.EmissionIntensity);
			propertyBlock.SetVector("_EmissionTex_Position", value2);
			Renderer.Sr.SetPropertyBlock(propertyBlock);
		}
		else
		{
			Renderer.Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
	}

	public void RefreshLightIntensity()
	{
		if (toggleOn && Renderer != null)
		{
			Renderer.GetLampRenderer().IntensityScale = GetIntensityFactor();
		}
	}
}
