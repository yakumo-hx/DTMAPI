using Cysharp.Threading.Tasks;
using DolocTown.Config.Weather;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(Image))]
public class WeatherTip : DolocBasicTip
{
	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private Image progressBarImage;

	[SerializeField]
	private Sprite normalIcon;

	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private Sprite malignantIcon;

	[SerializeField]
	private Color malignantColor;

	[SerializeField]
	private float fadeDuration = 0.2f;

	[SerializeField]
	private int fadeStep = 18;

	private Material material;

	private WeatherType currentType;

	private bool isInMalignantWeather;

	public Sprite Icon
	{
		set
		{
			iconImage.sprite = value;
		}
	}

	public Color IconColor
	{
		set
		{
			iconImage.color = value;
		}
	}

	public float Progress
	{
		set
		{
			material.SetFloat("_Progress", Mathf.Clamp01(value));
		}
	}

	private bool Clockwise
	{
		set
		{
			material.SetFloat("_Clockwise", value ? 1 : 0);
		}
	}

	private Color ProgressColor
	{
		set
		{
			material.SetColor("_Color", value);
		}
	}

	private WeatherSystem weatherSystem => DolocAPI.archiveHandle.WeatherSystem;

	private bool containsWeatherStation
	{
		get
		{
			if (DolocAPI.IsDataLoaded)
			{
				return DolocAPI.archiveHandle.MainFarm.ContainsWeatherStation;
			}
			return false;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		if (material == null)
		{
			material = new Material(progressBarImage.material);
			progressBarImage.material = material;
		}
	}

	protected override void OnHover()
	{
	}

	public override void SetVisible(bool value)
	{
		base.SetVisible(value && containsWeatherStation);
	}

	public void UpdateInfo()
	{
		UpdateWeatherNoRender(weatherSystem.WeatherType);
		UpdateWeatherProgress();
		iconImage.sprite = (weatherSystem.IsInMalignantWeather ? malignantIcon : normalIcon);
		ProgressColor = (weatherSystem.IsInMalignantWeather ? malignantColor : normalColor);
		Clockwise = weatherSystem.IsInMalignantWeather;
	}

	private void UpdateWeatherProgress()
	{
	}

	public void UpdateWeatherNoRender(WeatherType weatherType)
	{
		currentType = weatherType;
		if (isInMalignantWeather ^ weatherSystem.IsInMalignantWeather)
		{
			ProgressColor = (weatherSystem.IsInMalignantWeather ? malignantColor : normalColor);
			iconImage.sprite = (weatherSystem.IsInMalignantWeather ? malignantIcon : normalIcon);
			Clockwise = weatherSystem.IsInMalignantWeather;
		}
		isInMalignantWeather = weatherSystem.IsInMalignantWeather;
		UpdateWeatherProgress();
	}

	public void UpdateWeather(WeatherType weatherType)
	{
		currentType = weatherType;
		Clockwise = weatherSystem.IsInMalignantWeather;
		if (isInMalignantWeather ^ weatherSystem.IsInMalignantWeather)
		{
			Color dst = (weatherSystem.IsInMalignantWeather ? malignantColor : normalColor);
			DoProgress(material.color, dst, fadeStep, fadeDuration).Forget();
			iconImage.sprite = (weatherSystem.IsInMalignantWeather ? malignantIcon : normalIcon);
		}
		isInMalignantWeather = weatherSystem.IsInMalignantWeather;
	}

	private async UniTaskVoid DoProgress(Color src, Color dst, int step, float duration)
	{
		int delayTime = (int)(duration * 1000f / (float)step);
		Progress = -0.1f;
		await UniTask.Delay(delayTime);
		for (int i = 0; i <= step; i++)
		{
			float t = (Progress = (float)i * (1f / (float)step));
			ProgressColor = Color.Lerp(src, dst, t);
			await UniTask.Delay(delayTime);
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(material);
	}
}
