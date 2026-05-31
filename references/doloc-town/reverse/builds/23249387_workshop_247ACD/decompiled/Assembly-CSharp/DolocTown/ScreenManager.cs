using DolocTown.Config.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown;

public class ScreenManager
{
	private bool fullScreen;

	private RectTransform canvasOverlayRectTransform;

	private CanvasScaler canvasScaler;

	private int targetFrameRate;

	public Vector2Int defaultResolution { get; private set; }

	public Vector2Int worldResolution { get; private set; }

	public Vector2 screenSize => canvasOverlayRectTransform.rect.size;

	public Vector2 screenScale => canvasOverlayRectTransform.localScale;

	public float scaleFactor => canvasScaler.referenceResolution.x / (float)Screen.width;

	public ScreenManager(RectTransform canvasOverlayRectTransform)
	{
		defaultResolution = GetDefaultResolution();
		Debug.Log($"设备分辨率: {defaultResolution}");
		this.canvasOverlayRectTransform = canvasOverlayRectTransform;
		canvasScaler = canvasOverlayRectTransform.GetComponent<CanvasScaler>();
		InitSettings();
	}

	private Vector2Int GetDefaultResolution()
	{
		Display display = Display.main;
		Display[] displays = Display.displays;
		foreach (Display display2 in displays)
		{
			if (display2.active)
			{
				display = display2;
				break;
			}
		}
		Vector2Int result = new Vector2Int(display.systemWidth, display.systemHeight);
		float num = Mathf.Min((float)result.x / (float)result.y, 1.7777778f);
		result.x = Mathf.RoundToInt(num * (float)result.y);
		return result;
	}

	public void RegisterSettingListener()
	{
		DolocAPI.RegisterMsgListener(UserSettingType.GRAPHICS_RESOLUTION, RefreshResolutionBySetting);
		DolocAPI.RegisterMsgListener(UserSettingType.GRAPHICS_FULL_SCREEN, RefreshFullScreenBySetting);
		DolocAPI.RegisterMsgListener(UserSettingType.GRAPHICS_FPS, RefreshFPSBySetting);
		DolocAPI.RegisterMsgListener(UserSettingType.GRAPHICS_VSYNC, RefreshVSyncBySetting);
	}

	private void InitSettings()
	{
		RefreshFullScreenBySetting();
		RefreshResolutionBySetting(autoSetWindowed: false);
		RefreshFPSBySetting();
	}

	private void RefreshResolutionBySetting(object obj, GameEventArgs args)
	{
		RefreshResolutionBySetting(autoSetWindowed: true);
	}

	private void RefreshResolutionBySetting(bool autoSetWindowed)
	{
		worldResolution = GetResolutionSetting();
		Debug.Log($"刷新分辨率: {worldResolution}");
		Screen.SetResolution(worldResolution.x, worldResolution.y, fullScreen);
		if (DolocAPI.IsGameInitialized)
		{
			DolocAPI.cameraController.RefreshResolution();
		}
		if (DolocAPI.IsDataLoaded)
		{
			DolocAPI.CurrentRoom.SceneHandle?.ResetWaterResolutions(worldResolution);
		}
		if (autoSetWindowed)
		{
			bool flag = worldResolution.x < defaultResolution.x || worldResolution.y < defaultResolution.y;
			DolocAPI.userSettings.SetValue(UserSettingType.GRAPHICS_FULL_SCREEN, !flag);
		}
		RefreshFullScreenBySetting();
	}

	private void RefreshFullScreenBySetting(object obj = null, GameEventArgs args = null)
	{
		fullScreen = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.GRAPHICS_FULL_SCREEN);
		Screen.fullScreen = fullScreen;
		if (DolocAPI.IsGameInitialized)
		{
			DolocAPI.cameraController.RefreshResolution();
		}
	}

	private Vector2Int GetResolutionSetting()
	{
		string orDefault = DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.GRAPHICS_RESOLUTION);
		string[] array = orDefault.Split('×');
		if (array.IsNullOrEmpty())
		{
			array = orDefault.Split('x');
		}
		if (orDefault == "Auto" || array == null || array.Length != 2 || !int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2))
		{
			return defaultResolution;
		}
		return new Vector2Int(result, result2);
	}

	private void RefreshFPSBySetting(object obj = null, GameEventArgs args = null)
	{
		if (int.TryParse(DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.GRAPHICS_FPS), out var result))
		{
			if (result == 0)
			{
				targetFrameRate = Screen.currentResolution.refreshRate;
			}
			else
			{
				targetFrameRate = result;
			}
		}
		Application.targetFrameRate = targetFrameRate;
		RefreshVSyncBySetting();
	}

	private void RefreshVSyncBySetting(object obj = null, GameEventArgs args = null)
	{
		bool orDefault = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.GRAPHICS_VSYNC);
		int refreshRate = Screen.currentResolution.refreshRate;
		if (!orDefault)
		{
			QualitySettings.vSyncCount = 0;
		}
		else if (targetFrameRate > 0 && (float)targetFrameRate <= (float)refreshRate / 2f)
		{
			QualitySettings.vSyncCount = 2;
		}
		else
		{
			QualitySettings.vSyncCount = 1;
		}
	}
}
