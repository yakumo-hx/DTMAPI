using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using DolocTown;
using DolocTown.Config.Settings;
using DolocTown.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PPManager : MonoBehaviour
{
	[SerializeField]
	private Renderer2DData rendererData;

	[SerializeField]
	private Material flowPointsMat;

	[SerializeField]
	private Material MatFadeInOut;

	[SerializeField]
	private Material MatCircleDiffusion;

	[SerializeField]
	private Material MatCinemaScreen;

	[SerializeField]
	private Material MatGlobalFog;

	[SerializeField]
	private Material MatHeatWave;

	private VolumeProfile volume;

	private FadeInOutEx fadeInOutEx;

	private Vignette _vignette;

	private Color _vignetteColor;

	private UIAnimFloat vignetteIntensityAnim;

	private UIAnimFloat vignetteSmoothnessAnim;

	private Vector2 vignetteArgs;

	public bool DisableFadeInOnce;

	public bool DisableFadeOutOnce;

	private Dictionary<string, ScriptableRendererFeature> renderFeatures;

	public Material MaterialGlobalFog => MatGlobalFog;

	public MaterialSetter01 CinemaScreen { get; private set; }

	public MaterialSetter01 CircleDiffusion { get; private set; }

	public MaterialSetter01 TransitVertical { get; private set; }

	public MaterialSetter01 GlobalFog { get; private set; }

	public MaterialSetter01 HeatWave { get; private set; }

	public float FlowPointsLightness
	{
		get
		{
			return flowPointsMat.GetFloat("_Lightness");
		}
		set
		{
			flowPointsMat.SetFloat("_Lightness", value);
		}
	}

	public void Init()
	{
		renderFeatures = new Dictionary<string, ScriptableRendererFeature>();
		foreach (ScriptableRendererFeature rendererFeature in rendererData.rendererFeatures)
		{
			renderFeatures.Add(rendererFeature.name, rendererFeature);
		}
		fadeInOutEx = new FadeInOutEx(MatFadeInOut);
		SetEnabled(PPTypes.FADEINOUT, value: false);
		SetEnabled(PPTypes.RESIDENTFILTER, value: false);
		volume = GetComponent<Volume>().profile;
		volume.TryGet<Vignette>(out _vignette);
		vignetteArgs = new Vector2(_vignette.intensity.value, _vignette.smoothness.value);
		vignetteIntensityAnim = new UIAnimFloat(() => _vignette.intensity.value, delegate(float v)
		{
			_vignette.intensity.value = v;
		});
		vignetteSmoothnessAnim = new UIAnimFloat(() => _vignette.smoothness.value, delegate(float v)
		{
			_vignette.smoothness.value = v;
		});
		_vignetteColor = _vignette.color.value;
		CircleDiffusion = new MaterialSetter01(MatCircleDiffusion, "_Lightness");
		CinemaScreen = new MaterialSetter01(MatCinemaScreen, "_Process");
		TransitVertical = new MaterialSetter01(MatFadeInOut, "_Progress");
		GlobalFog = new MaterialSetter01(MatGlobalFog, "_Intensity");
		HeatWave = new MaterialSetter01(MatHeatWave, "_Process");
	}

	public void ReturnHome()
	{
		SetEnabled(PPTypes.RESIDENTFILTER, value: false);
		SetEnabled(PPTypes.GLOBALFOG, value: false);
	}

	public void SetEnabled(PPTypes index, bool value)
	{
		string key = index.ToString();
		if (renderFeatures.ContainsKey(key) && renderFeatures[key].isActive != value)
		{
			renderFeatures[key].SetActive(value);
		}
	}

	public void SwitchPPEnabled(PPTypes type)
	{
		SetEnabled(type, !renderFeatures[type.ToString()].isActive);
	}

	public void VignetteFadeInout(float intensity, float smoothness, Color c, float time = 1f)
	{
		vignetteIntensityAnim.time = time;
		vignetteIntensityAnim.to = intensity;
		vignetteSmoothnessAnim.time = time;
		vignetteSmoothnessAnim.to = smoothness;
		vignetteIntensityAnim.play();
		vignetteSmoothnessAnim.play();
	}

	public void VignetteFadeInoutResume(float time)
	{
		VignetteFadeInout(vignetteArgs.x, vignetteArgs.y, _vignetteColor, time);
	}

	public void FadeIn(float time, Action callback = null, bool shouldReset = false)
	{
		if (DisableFadeInOnce)
		{
			DisableFadeInOnce = false;
			callback?.Invoke();
			return;
		}
		if (shouldReset)
		{
			MatFadeInOut.SetFloat("_Lightness", 1f);
		}
		SetEnabled(PPTypes.FADEINOUT, value: true);
		fadeInOutEx.FadeIn(time, delegate
		{
			callback?.Invoke();
		});
	}

	public void FadeOut(float time, Action callback = null, bool shouldReset = false, [CallerMemberName] string caller = null)
	{
		Debug.Log("FadeOut called by " + caller);
		if (DisableFadeOutOnce)
		{
			DisableFadeOutOnce = false;
			callback?.Invoke();
			return;
		}
		if (shouldReset)
		{
			MatFadeInOut.SetFloat("_Lightness", 0f);
		}
		fadeInOutEx.FadeOut(time, delegate
		{
			callback?.Invoke();
			SetEnabled(PPTypes.FADEINOUT, value: false);
		});
	}

	public void CircleFadeIn(float time, Action callback = null, Ease ease = Ease.Linear)
	{
		MatCircleDiffusion.SetFloat("_Lightness", 1f);
		SetEnabled(PPTypes.CIRCLEDIFFUSION, value: true);
		CircleDiffusion.Play(0f, time, ease, delegate
		{
			callback?.Invoke();
		});
	}

	public void CircleFadeOut(float time, Action callback = null, Ease ease = Ease.Linear)
	{
		MatCircleDiffusion.SetFloat("_Lightness", 0f);
		CircleDiffusion.Play(1f, time, ease, delegate
		{
			SetEnabled(PPTypes.CIRCLEDIFFUSION, value: false);
			callback?.Invoke();
		});
	}

	public void ApplyCustomFilter(Material material)
	{
		((MyBlitFeature)renderFeatures[PPTypes.CUSTOMFILTER.ToString()]).SetMaterial(material);
	}

	private void ClearRoomPostProcessingEffects()
	{
		ScriptableRendererFeature scriptableRendererFeature = renderFeatures[PPTypes.ROOMPOSTPROCESSING.ToString()];
		((CustomPostProcessingGroup)scriptableRendererFeature).SetMaterials(Array.Empty<Material>());
		scriptableRendererFeature.SetActive(active: false);
	}

	public void SetupRoomPostProcessingEffects(Material[] materials)
	{
		if (materials.IsNullOrEmpty())
		{
			ClearRoomPostProcessingEffects();
			return;
		}
		materials = materials.Where((Material x) => x != null).ToArray();
		CustomPostProcessingGroup obj = (CustomPostProcessingGroup)renderFeatures[PPTypes.ROOMPOSTPROCESSING.ToString()];
		obj.SetMaterials(materials);
		obj.SetActive(active: true);
	}

	public void AfterLoadArchiveData()
	{
		_OnGraphicsFilterChanged();
	}

	public void DisableResidentFilter()
	{
		SetEnabled(PPTypes.RESIDENTFILTER, value: false);
	}

	public void ResumeResidentFilter()
	{
		_OnGraphicsFilterChanged();
	}

	public void RegisterSettingsListener()
	{
		DolocAPI.RegisterMsgListener(UserSettingType.GRAPHICS_FILTER, delegate
		{
			_OnGraphicsFilterChanged();
		});
	}

	public void RefreshResidentFilter()
	{
		_OnGraphicsFilterChanged();
		SetEnabled(PPTypes.RESIDENTFILTER, value: false);
	}

	private void _OnGraphicsFilterChanged()
	{
		string orDefault = DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.GRAPHICS_FILTER);
		SetGameFilter(GetFilterMaterial(orDefault));
	}

	private Material GetFilterMaterial(string name)
	{
		return name switch
		{
			"None" => null, 
			"RetroCrt" => LocMaterials.GAME_MAT_PP_RESIDENT_TELEVISION, 
			"RetroCrtBW" => LocMaterials.GAME_MAT_PP_TELEVISION_GREY_SCALE, 
			"BW" => LocMaterials.GAME_MAT_PP_GREY_SCALE, 
			_ => null, 
		};
	}

	public void SetGameFilter(Material material = null)
	{
		if (material == null)
		{
			SetEnabled(PPTypes.RESIDENTFILTER, value: false);
			return;
		}
		SetEnabled(PPTypes.RESIDENTFILTER, value: true);
		((MyBlitFeature)renderFeatures[PPTypes.RESIDENTFILTER.ToString()]).SetMaterial(material);
	}
}
