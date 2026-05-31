using System;
using System.Globalization;
using System.IO;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Settings;
using DolocTown.GameData;
using DolocTown.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
	[SerializeField]
	public Transform effectsContainer;

	[SerializeField]
	public Transform globalContainer;

	[SerializeField]
	public Transform farmContaier;

	[SerializeField]
	public Transform cinemaContainer;

	[SerializeField]
	public int archiveFileCount = 6;

	[SerializeField]
	public string archiveFileNameFormat = "doloc-archive-{0}.data";

	[SerializeField]
	public string minimumSupportedVersion = "0.91.30";

	[SerializeField]
	public bool useEncryption;

	[SerializeField]
	public string encryptPrefix;

	[SerializeField]
	public string aesKey;

	[SerializeField]
	public string aesIV;

	[SerializeField]
	private GameInitConfig devInitConfig;

	[SerializeField]
	private GameInitConfig buildInitConfig;

	[SerializeField]
	private GameOuterConfigSO defaultOuterConfig;

	private EventSystem eventSystem;

	public GameOuterConfig gameOuterConfig { get; private set; }

	public GameInitConfig gameInitConfig => buildInitConfig;

	public bool shouldCheckStateSwitch => !gameInitConfig.ignoreStateChangeMsg;

	public bool logDialogueCommandDebugInfo => !gameInitConfig.ignoreDialogueCommandDebugInfo;

	public bool shouldBuilderCostAssets => !gameInitConfig.ignoreMaterialCost;

	protected bool IsGlobalContainerValid
	{
		get
		{
			if (globalContainer == null)
			{
				Debug.LogError("全局容器不得为空");
				return false;
			}
			if (globalContainer.GetComponent<Grid>() == null)
			{
				Debug.LogError("全局容器必须挂载Grid组件");
				return false;
			}
			return true;
		}
	}

	public bool IsGameInitialized { get; private set; }

	public SteamForDolocTown steamForDolocTown { get; private set; }

	public bool IsSteamEnabled
	{
		get
		{
			if (steamForDolocTown != null)
			{
				return steamForDolocTown.EnableSteamSDK;
			}
			return false;
		}
	}

	public bool IsSteamSDKInitialized
	{
		get
		{
			if (steamForDolocTown != null)
			{
				return steamForDolocTown.isSteamSDKInitialized;
			}
			return false;
		}
	}

	public bool DisableSteamValidator
	{
		get
		{
			if (!gameInitConfig.disableSteamValidator)
			{
				return gameOuterConfig.disableSteamValidator;
			}
			return true;
		}
	}

	private bool isFarmRendererValid(Transform farmRenderer)
	{
		if (farmRenderer == null)
		{
			Debug.LogError("农场渲染器不得为空");
			return false;
		}
		if (farmRenderer.GetComponent<Grid>() == null)
		{
			Debug.LogError("农场渲染器必须挂载Grid组件");
			return false;
		}
		return true;
	}

	private bool isWorldRendererValid(Transform worldRenderer)
	{
		if (worldRenderer == null)
		{
			Debug.LogError("世界渲染器不得为空");
			return false;
		}
		return true;
	}

	private bool isCinemaRendererValid(Transform cinemaRenderer)
	{
		if (cinemaRenderer == null)
		{
			Debug.LogError("过场动画渲染器不得为空");
			return false;
		}
		return true;
	}

	private void Awake()
	{
		BeforeAwake();
		DolocAPI.gameManager = this;
		DolocAPI.InitDevelopmentHelper();
		DolocAPI.__InitGlobalSystems();
		DolocAPI.__LoadStaticAssets(InitGameSystems);
	}

	private void BeforeAwake()
	{
		ReloadOuterConfig();
		steamForDolocTown = GetComponent<SteamForDolocTown>();
		if (steamForDolocTown != null)
		{
			steamForDolocTown.Init();
		}
		CultureInfo.DefaultThreadCurrentUICulture = (CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("zh-CN"));
	}

	private void LoadLanguageFromSteam()
	{
		if (DolocAPI.gameManager.IsSteamSDKInitialized && DolocAPI.IsFirstPlayGame(shouldMark: false) && steamForDolocTown != null && steamForDolocTown.EnableSteamSDK)
		{
			Debug.Log("读取SteamSDK语言配置:" + steamForDolocTown.Language);
			DolocAPI.userSettings.SetValue(UserSettingType.LANGUAGE_TEXT, steamForDolocTown.LanguageForDolocTown);
			DolocAPI.SaveUserSettings();
		}
	}

	private void InitGameSystems()
	{
		DolocAPI.outputSuccess("准备初始化所有游戏系统..");
		DolocAPI.__InstallConfigs();
		DolocAPI.__InitDolocBaseGameSystems(this);
		DolocAPI.__InitFarmSystem(this);
		DolocAPI.__InitCitySystem(this);
		DolocAPI.__InitDungeonSystem(this);
		DolocAPI.__InstallStateSwitchPlugins();
		OnEverythingLoaded();
	}

	private void OnEverythingLoaded()
	{
		IsGameInitialized = true;
		DolocAPI.UserInput.LoadBindingOverrides();
		if (gameInitConfig.resetUserSettings)
		{
			DolocAPI.ResetUserSettings();
			DolocAPI.SaveUserSettings(new UserSettings());
		}
		DolocAPI.Sound.OnEverythingLoaded();
		LoadLanguageFromSteam();
		DolocAPI.userSettings.OnEverythingLoaded();
		DolocAPI.userSettings.UpdateCachedData();
		if (DolocAPI.UseMods)
		{
			DolocConfig.Reload();
		}
		DolocAPI.SwitchLanguage(DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.LANGUAGE_TEXT));
		DolocAPI.screenManager.RegisterSettingListener();
		DolocAPI.ppm.RegisterSettingsListener();
		DolocAPI.FontManager.RegisterUserSettingsListener();
		DolocAPI.cameraController.RefreshResolution();
		DolocAPI.gameLoop.SetEnabled(value: true);
		DolocAPI.WaitUntil(() => !SplashController.InSplash, HandleReturnHome);
	}

	private void HandleReturnHome()
	{
		if (DolocAPI.IsFirstPlayGame(shouldMark: false))
		{
			QuestionDataCollector(delegate
			{
				DolocAPI.ReturnHome(gameInitConfig.skipArchiveData);
			});
		}
		else
		{
			DolocAPI.ReturnHome(gameInitConfig.skipArchiveData);
		}
		DolocAPI.IsFirstPlayGame(shouldMark: true);
	}

	private void QuestionDataCollector(Action callBack)
	{
		DolocAPI.EnterUI((LargeQuesitionUiState s) => s.HandleStartUpArgs(DolocConfig.StaticTexts.SettingPanelAllowTracedataCollector, DolocConfig.StaticTexts.SettingPanelTracedataCollectorDesc, delegate
		{
			DolocAPI.userSettings.SetValue(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR, true);
			DolocAPI.SaveUserSettings();
			callBack?.Invoke();
		}, delegate
		{
			DolocAPI.userSettings.SetValue(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR, false);
			DolocAPI.SaveUserSettings();
			callBack?.Invoke();
		}, firstSelectedConfirm: true));
	}

	private void ReloadOuterConfig()
	{
		string path = Path.Join(Application.persistentDataPath, "config.json");
		if (File.Exists(path))
		{
			string json = File.ReadAllText(path);
			gameOuterConfig = GameOuterConfig.LoadFromJson(json, defaultOuterConfig);
		}
		else
		{
			gameOuterConfig = defaultOuterConfig.GetGameOuterConfig();
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (!steamForDolocTown.isSteamSDKInitialized || !SteamUtils.IsSteamRunningOnSteamDeck())
		{
			if (hasFocus)
			{
				DolocAPI.UserInput?.ResumeCurrentInput();
			}
			else
			{
				DolocAPI.UserInput?.DisableAllInput(includeGlobal: true);
			}
			if ((object)eventSystem == null)
			{
				eventSystem = EventSystem.current;
			}
			if (eventSystem != null)
			{
				eventSystem.enabled = hasFocus;
			}
		}
	}

	private void OnApplicationQuit()
	{
		Debug.Log("游戏关闭..");
		if (!(DolocAPI.ppm != null))
		{
			return;
		}
		foreach (object value in Enum.GetValues(typeof(PPTypes)))
		{
			DolocAPI.ppm.SetEnabled((PPTypes)value, value: false);
		}
	}
}
