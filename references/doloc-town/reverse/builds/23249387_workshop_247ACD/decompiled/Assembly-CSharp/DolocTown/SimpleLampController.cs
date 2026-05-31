using DG.Tweening;
using DolocTown.Config.Equipment;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

public class SimpleLampController : LampControllerBase
{
	private readonly SpriteRenderer sr;

	private readonly GameEntitySlot<LampRenderer> lampRendererSlot = new GameEntitySlot<LampRenderer>();

	public SimpleLampController(SpriteRenderer sr, LampInfo lampInfo)
		: base(lampInfo)
	{
		this.sr = sr;
	}

	public override void ToggleLight(bool value)
	{
		ToggleMat(value);
		if (value)
		{
			LampRenderer entity = lampRendererSlot.Entity;
			if (entity != null)
			{
				entity.Render(lampInfo, sr.transform.position);
				entity.IntensityScale = 1f;
				entity.Color = Color.white;
			}
		}
		else
		{
			lampRendererSlot.Release();
		}
	}

	public override void ToggleLight(bool value, float time)
	{
		if (value)
		{
			ToggleMat(value: true);
			LampRenderer entity = lampRendererSlot.Entity;
			if (entity != null)
			{
				entity.Render(lampInfo, sr.transform.position);
			}
			FadeFunc(0f);
			noiseTween?.Kill();
			noiseTween = DOTween.To(FadeFunc, 0f, 1f, time);
		}
		else
		{
			FadeFunc(1f);
			noiseTween?.Kill();
			noiseTween = DOTween.To(FadeFunc, 1f, 0f, time).OnComplete(delegate
			{
				ToggleMat(value: false);
				lampRendererSlot.Release();
			});
		}
	}

	public void Dispose()
	{
		StopTween();
		ToggleMat(value: false);
		lampRendererSlot.Release();
	}

	protected override void ToggleMat(bool value)
	{
		if (value)
		{
			sr.material = DolocAPI.GetAsset<Material>(DolocGameAssets.GAME_MAT_SPEC_SIGNAL_LIGHT);
			Vector4 outerUV = DataUtility.GetOuterUV(sr.sprite);
			Vector4 outerUV2 = DataUtility.GetOuterUV(EmissionSprite);
			Vector4 value2 = new Vector4(outerUV2.x, outerUV2.y, outerUV.x, outerUV.y);
			sr.material.SetTexture("_MainTex", sr.sprite.texture);
			sr.material.SetTexture("_EmissionTex", EmissionSprite.texture);
			sr.material.SetColor("_EmissionColor", lampInfo.EmissionColor);
			propertyBlock.SetFloat("_EmissionIntensity", lampInfo.EmissionIntensity);
			sr.material.SetVector("_UvCoordinates", value2);
		}
		else
		{
			Object.Destroy(sr.material);
			sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
		}
	}

	private void FadeFunc(float v)
	{
		LampRenderer lampRenderer = lampRendererSlot.Fetch();
		if (!(lampRenderer == null))
		{
			lampRenderer.IntensityScale = v;
			sr.material.SetFloat("_ShineIntensity", v);
		}
	}
}
