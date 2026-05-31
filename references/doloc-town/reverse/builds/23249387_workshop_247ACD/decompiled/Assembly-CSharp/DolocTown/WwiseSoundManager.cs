using System;
using System.Collections.Generic;
using AK.Wwise.Unity.WwiseAddressables;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.Config.Settings;
using DolocTown.Config.Sound;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using RedSaw;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DolocTown;

public class WwiseSoundManager : DolocObject
{
	private struct SfxStamp
	{
		public string eventName;

		public float time;

		public SfxStamp(string eventName, float time)
		{
			this.eventName = eventName;
			this.time = time;
		}
	}

	private string currentAmbienceEvent;

	private string currentBgmEventName;

	private string overideBgmEventName;

	private bool isBgmPlaying;

	private bool isBgmPaused;

	private bool shouldDelayBgm;

	private RSTimer bgmDelayTimer;

	private RSTimer bgmTimeOutTimer = new RSTimer();

	private float _globalVolumeRatio = 1f;

	private Dictionary<SoundBusses, float> volumes = new Dictionary<SoundBusses, float>();

	private HashSet<string> loadedSoundBanks = new HashSet<string>();

	private HashSet<GameObject> registeredObjs = new HashSet<GameObject>();

	private bool bgmSwitchState;

	private Dictionary<string, SfxStamp> ocupiedGroup = new Dictionary<string, SfxStamp>();

	private static Dictionary<string, string> soundEventLUT = new Dictionary<string, string>
	{
		{ "__HOLD_PLACE__", "SFX" },
		{ "NEW_EVENT", "SFX" },
		{ "PLAY_ANIMAL_DROP", "SFX" },
		{ "PLAY_ANIMAL_EAT", "SFX" },
		{ "PLAY_ANIMAL_JUMP", "SFX" },
		{ "PLAY_ANIMAL_PET_CHICKEN", "SFX" },
		{ "PLAY_ANIMAL_PET_CHICKEN_CHILD", "SFX" },
		{ "PLAY_ANIMAL_PET_PANGOLIN", "SFX" },
		{ "PLAY_ANIMAL_PET_SHEEP", "SFX" },
		{ "PLAY_ANIMAL_PET_SHEEP_CHILD", "SFX" },
		{ "PLAY_ANIMAL_PET_SLIME", "SFX" },
		{ "PLAY_ANIMAL_SACK_CATCH", "SFX" },
		{ "PLAY_ANIMAL_SACK_RELEASE", "SFX" },
		{ "PLAY_ANIMAL_WALK_FOOT", "SFX" },
		{ "PLAY_ANIMAL_WALK_HOOF", "SFX" },
		{ "PLAY_ANIMAL_WALK_SLIME", "SFX" },
		{ "PLAY_CHARACTER_ATTACK", "SFX" },
		{ "PLAY_CHARACTER_ATTACK_SIMPLE", "SFX" },
		{ "PLAY_CHARACTER_COLLECT_BERRY", "SFX" },
		{ "PLAY_CHARACTER_DASH", "SFX" },
		{ "PLAY_CHARACTER_DIAL_01", "SFX" },
		{ "PLAY_CHARACTER_DIAL_02", "SFX" },
		{ "PLAY_CHARACTER_DIAL_03", "SFX" },
		{ "PLAY_CHARACTER_DIAL_04", "SFX" },
		{ "PLAY_CHARACTER_DIAL_05", "SFX" },
		{ "PLAY_CHARACTER_DOUBLE_JUMP", "SFX" },
		{ "PLAY_CHARACTER_DRINK", "SFX" },
		{ "PLAY_CHARACTER_EAT", "SFX" },
		{ "PLAY_CHARACTER_ENTER_BUILDING", "SFX" },
		{ "PLAY_CHARACTER_ENTER_BUILDING_ERROR", "SFX" },
		{ "PLAY_CHARACTER_ENTER_WEEDS", "SFX" },
		{ "PLAY_CHARACTER_FERTILIZE", "SFX" },
		{ "PLAY_CHARACTER_FOOTSTEP", "SFX" },
		{ "PLAY_CHARACTER_FOOTSTEP_DROP", "SFX" },
		{ "PLAY_CHARACTER_GUN_RELOAD", "SFX" },
		{ "PLAY_CHARACTER_HARVEST", "SFX" },
		{ "PLAY_CHARACTER_HURT", "SFX" },
		{ "PLAY_CHARACTER_JUMP", "SFX" },
		{ "PLAY_CHARACTER_PICK_UP_TELEPHONE", "SFX" },
		{ "PLAY_CHARACTER_PLANT", "SFX" },
		{ "PLAY_CHARACTER_SEARCHING_TRASHCAN", "SFX" },
		{ "PLAY_CHARACTER_SHOVEL", "SFX" },
		{ "PLAY_CHARACTER_SLEEP", "SFX" },
		{ "PLAY_CHARACTER_SUPER_JUMP", "SFX" },
		{ "PLAY_CHARACTER_SWITCH_OFF", "SFX" },
		{ "PLAY_CHARACTER_SWITCH_ON", "SFX" },
		{ "PLAY_CHARACTER_TELEPORT", "SFX" },
		{ "PLAY_CHARACTER_WATERING", "SFX" },
		{ "PLAY_CHARACTER_YAWN", "SFX" },
		{ "PLAY_CHARATER_FOOTSTEP_WATER", "SFX" },
		{ "PLAY_CHARATER_LUMBER", "SFX" },
		{ "PLAY_DRONE_ATTACK", "SFX" },
		{ "PLAY_DRONE_ATTACK_BOMB_EXPLOSIVE", "SFX" },
		{ "PLAY_DRONE_ATTACK_THROW_BOMB", "SFX" },
		{ "PLAY_DRONE_BEE_ATTACK", "SFX" },
		{ "PLAY_DRONE_BEE_CHARGE", "SFX" },
		{ "PLAY_DRONE_BEE_HUMMING", "SFX" },
		{ "PLAY_DRONE_BEE_PRE", "SFX" },
		{ "PLAY_DRONE_BOMB_COUNT_DOWN", "SFX" },
		{ "PLAY_DRONE_HUMMING", "SFX" },
		{ "PLAY_ENTER_HIDDEN_ROOM", "SFX" },
		{ "PLAY_ENTER_WATER", "SFX" },
		{ "PLAY_EQUIPMENT_WELL_DRAW_WATER", "SFX" },
		{ "PLAY_FISHING_BONUS_HINT", "SFX" },
		{ "PLAY_FISHING_ERROR_HINT", "SFX" },
		{ "PLAY_FISHING_FAILED", "SFX" },
		{ "PLAY_FISHING_FISH_BITE", "SFX" },
		{ "PLAY_FISHING_HOOK_SPLASH", "SFX" },
		{ "PLAY_FISHING_REEL_IN", "SFX" },
		{ "PLAY_FISHING_REEL_OUT", "SFX" },
		{ "PLAY_FISHING_SUCCESS", "SFX" },
		{ "PLAY_FISHING_THROW_HOOK", "SFX" },
		{ "PLAY_GARBAGE_COLLECT", "SFX" },
		{ "PLAY_GARBAGE_RECYCLING", "SFX" },
		{ "PLAY_ITEM_BOX", "SFX" },
		{ "PLAY_ITEM_PICK_UP", "SFX" },
		{ "PLAY_KNOCKING_EQUIPMENT", "SFX" },
		{ "PLAY_LIGHTNING", "SFX" },
		{ "PLAY_NEW_EMAIL", "SFX" },
		{ "PLAY_NPC_TALK", "SFX" },
		{ "PLAY_OBJECT_EVAPORATION", "SFX" },
		{ "PLAY_OBJECT_LIGHTBULB_BUZZING", "SFX" },
		{ "PLAY_PLACE_EQUIPMENT_BIG", "SFX" },
		{ "PLAY_PLACE_EQUIPMENT_SMALL", "SFX" },
		{ "PLAY_PLANT_ANALYSING", "SFX" },
		{ "PLAY_RESOURCE_FELL", "SFX" },
		{ "PLAY_RESOURCE_FELL_ERROR", "SFX" },
		{ "PLAY_RESOURCE_GATHER", "SFX" },
		{ "PLAY_RESOURCE_GATHER_ERROR", "SFX" },
		{ "PLAY_RESOURCE_HARVEST", "SFX" },
		{ "PLAY_RESOURCE_PAPER_BOX", "SFX" },
		{ "PLAY_SLIME_DEAD", "SFX" },
		{ "PLAY_SLIME_HURT", "SFX" },
		{ "PLAY_SLIME_JUMP", "SFX" },
		{ "PLAY_SLIME_MOVEMENT", "SFX" },
		{ "PLAY_TELEPHONE_INPUT_END", "SFX" },
		{ "PLAY_TELEPHONE_INPUT_INVALID", "SFX" },
		{ "PLAY_UI_BACKPACK_TIDY_UP", "SFX" },
		{ "PLAY_UI_CANCEL", "SFX" },
		{ "PLAY_UI_CLICK", "SFX" },
		{ "PLAY_UI_CONFIRM", "SFX" },
		{ "PLAY_UI_ERROR", "SFX" },
		{ "PLAY_UI_ERROR_03", "SFX" },
		{ "PLAY_UI_GOLD_ROLL", "SFX" },
		{ "PLAY_UI_ITEM_DELETE", "SFX" },
		{ "PLAY_UI_MAP", "SFX" },
		{ "PLAY_UI_MAP_CLOSE", "SFX" },
		{ "PLAY_UI_MISSION_MESSAGE", "SFX" },
		{ "PLAY_UI_PAGE", "SFX" },
		{ "PLAY_UI_PICK_UP_ITEM", "SFX" },
		{ "PLAY_UI_POP_DOWN", "SFX" },
		{ "PLAY_UI_POP_UP", "SFX" },
		{ "PLAY_UI_SELECT", "SFX" },
		{ "PLAY_UI_TIPS_POPUP", "SFX" },
		{ "PLAY_UI_WATER_BUBBLES", "SFX" },
		{ "PLAY_WATER_COLLECT", "SFX" },
		{ "STOP_CHARACTER_FOOTSTEP", "SFX" },
		{ "STOP_CHARACTER_USE_TOOL", "SFX" },
		{ "STOP_CHARACTER_YAWN", "SFX" },
		{ "STOP_DRONE_BOMB_COUNT_DOWN", "SFX" },
		{ "STOP_DRONE_HUMMING", "SFX" },
		{ "STOP_FISHING_REEL", "SFX" },
		{ "STOP_FISHING_REEL_IN", "SFX" },
		{ "STOP_FISHING_REEL_OUT", "SFX" },
		{ "STOP_GARBAGE_RECYCLING", "SFX" },
		{ "STOP_MONSTER_BUS", "SFX" },
		{ "STOP_NPC_TALK", "SFX" },
		{ "STOP_PLANT_ANALYSING", "SFX" },
		{ "STOP_SLIME_MOVEMENT", "SFX" },
		{ "STOP_UI_GOLD_ROLL", "SFX" },
		{ "PLAY_AMBIENCE_HEAVY_RAIN_INDOOR", "MUSIC" },
		{ "PLAY_AMBIENCE_HEAVY_RAIN_OUTSIDE", "MUSIC" },
		{ "PLAY_AMBIENCE_RAIN_INDOOR", "MUSIC" },
		{ "PLAY_AMBIENCE_RAIN_OUTSIDE", "MUSIC" },
		{ "PLAY_AMBIENCE_WINDY_INDOOR", "MUSIC" },
		{ "PLAY_AMBIENCE_WINDY_OUTSIDE", "MUSIC" },
		{ "PLAY_BGM_01", "MUSIC" },
		{ "PLAY_BGM_02", "MUSIC" },
		{ "PLAY_BGM_03", "MUSIC" },
		{ "PLAY_BGM_04", "MUSIC" },
		{ "PLAY_BGM_05", "MUSIC" },
		{ "PLAY_BGM_06", "MUSIC" },
		{ "PLAY_BGM_07", "MUSIC" },
		{ "PLAY_BGM_08", "MUSIC" },
		{ "PLAY_BGM_09", "MUSIC" },
		{ "PLAY_BGM_10", "MUSIC" },
		{ "PLAY_BGM_11", "MUSIC" },
		{ "PLAY_BGM_12", "MUSIC" },
		{ "PLAY_BGM_13", "MUSIC" },
		{ "PLAY_BGM_14", "MUSIC" },
		{ "PLAY_BGM_15", "MUSIC" },
		{ "PLAY_BGM_16", "MUSIC" },
		{ "PLAY_BGM_17", "MUSIC" },
		{ "PLAY_BGM_18", "MUSIC" },
		{ "PLAY_BGM_19", "MUSIC" },
		{ "PLAY_BGM_20", "MUSIC" },
		{ "PLAY_BGM_21", "MUSIC" },
		{ "PLAY_BGM_22", "MUSIC" },
		{ "PLAY_BGM_23", "MUSIC" },
		{ "PLAY_BGM_24", "MUSIC" },
		{ "PLAY_BGM_25", "MUSIC" },
		{ "PLAY_BGM_26", "MUSIC" },
		{ "PLAY_BGM_27", "MUSIC" },
		{ "PLAY_BGM_ACID_RAIN", "MUSIC" },
		{ "PLAY_BGM_BAR", "MUSIC" },
		{ "PLAY_BGM_CITY", "MUSIC" },
		{ "PLAY_BGM_DUNGEON", "MUSIC" },
		{ "PLAY_BGM_EVERNIGHT_NEWYEAR", "MUSIC" },
		{ "PLAY_BGM_EVERNIGHT_WORSHIP", "MUSIC" },
		{ "PLAY_BGM_FARM", "MUSIC" },
		{ "PLAY_BGM_FIRST_ANIM", "MUSIC" },
		{ "PLAY_BGM_HOME", "MUSIC" },
		{ "PLAY_BGM_NIGHT", "MUSIC" },
		{ "PLAY_BGM_RAIN", "MUSIC" },
		{ "STOP_AMBIENCE", "MUSIC" },
		{ "STOP_BGM", "MUSIC" },
		{ "STOP_BGM_IMMEDIATELY", "MUSIC" }
	};

	private GameObject agentObj => DolocAPI.agent.gameObject;

	private bool useLog => DolocAPI.gameManager.gameInitConfig.enableWwiseLog;

	public float GlobalVolumeRatio
	{
		get
		{
			return _globalVolumeRatio;
		}
		set
		{
			_globalVolumeRatio = Mathf.Max(0f, value);
			volumes[SoundBusses.MASTER_AUDIO_BUS] = GlobalVolumeRatio * (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.VOLUME_GLOBAL) / 100f;
		}
	}

	private TbExclusiveSFX exclusiveGroupTable => DolocConfig.Tables.TbExclusiveSFX;

	private float sfxGroupInterval => DolocAPI.GlobalParameter.SfxGroupInterval;

	public void RefreshAmbience()
	{
		bool is_indoor = DolocAPI.CurrentRoom?.IsInHouse ?? false;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		PlayAmbience(DolocConfig.Tables.TbAmbience.Get(is_indoor, currentWeatherType)?.SoundEvent ?? currentAmbienceEvent);
	}

	public void PlayAmbience(SoundEvents soundEvent)
	{
		PlayAmbience(soundEvent.ToString());
	}

	public void StopAmbience()
	{
		currentAmbienceEvent = string.Empty;
		PostSoundEvent(SoundEvents.STOP_AMBIENCE);
	}

	public void PlayAmbience(string soundEvent)
	{
		if (!soundEvent.IsNullOrEmpty() && !(currentAmbienceEvent == soundEvent))
		{
			StopAmbience();
			if (useLog)
			{
				Debug.Log("切换环境音: " + soundEvent);
			}
			currentAmbienceEvent = soundEvent;
			PostSoundEvent(currentAmbienceEvent);
		}
	}

	public void RefreshBgmLater()
	{
		shouldDelayBgm = true;
		isBgmPaused = false;
		overideBgmEventName = string.Empty;
	}

	public void RefreshBgmImmediately()
	{
		shouldDelayBgm = true;
		isBgmPaused = false;
		overideBgmEventName = string.Empty;
		string currentEnvironmentBgm = GetCurrentEnvironmentBgm();
		if (!(currentEnvironmentBgm == currentBgmEventName))
		{
			currentBgmEventName = currentEnvironmentBgm;
			StopCurrentBgmBeforePlay();
			RefreshNextBgm(tryDelay: false);
		}
	}

	public void PlayBgm(string soundEvent)
	{
		if (soundEvent.IsNullOrEmpty())
		{
			RefreshBgmImmediately();
			return;
		}
		shouldDelayBgm = false;
		isBgmPaused = false;
		if (!isBgmPlaying || !(overideBgmEventName == soundEvent))
		{
			overideBgmEventName = soundEvent;
			StopCurrentBgmBeforePlay();
			RefreshNextBgm(tryDelay: false);
		}
	}

	private void StopCurrentBgmBeforePlay()
	{
		bgmTimeOutTimer.Reset();
		if (useLog)
		{
			Debug.Log("暂停当前BGM");
		}
		bgmDelayTimer = null;
		PostSoundEvent(SoundEvents.STOP_BGM);
	}

	public void StopBgm()
	{
		bgmTimeOutTimer.Reset();
		isBgmPaused = true;
		if (useLog)
		{
			Debug.Log("停止BGM");
		}
		bgmDelayTimer = null;
		PostSoundEvent(SoundEvents.STOP_BGM);
		currentBgmEventName = string.Empty;
		overideBgmEventName = string.Empty;
	}

	private void RefreshNextBgm(bool tryDelay)
	{
		if (isBgmPaused || isBgmPlaying || bgmDelayTimer != null)
		{
			return;
		}
		if (shouldDelayBgm && tryDelay)
		{
			Vector2 bgmRandomIdleTime = DolocAPI.GlobalParameter.BgmRandomIdleTime;
			bgmDelayTimer = new RSTimer(UnityEngine.Random.Range(bgmRandomIdleTime.x, bgmRandomIdleTime.y));
		}
		if (!overideBgmEventName.IsNullOrEmpty())
		{
			PlayBGMInternal(overideBgmEventName);
			return;
		}
		string currentEnvironmentBgm = GetCurrentEnvironmentBgm();
		string text = (currentEnvironmentBgm.IsNullOrEmpty() ? currentBgmEventName : currentEnvironmentBgm);
		if (!text.IsNullOrEmpty())
		{
			PlayBGMInternal(text);
		}
	}

	private string GetCurrentEnvironmentBgm()
	{
		if (!DolocAPI.IsDataLoaded || DolocAPI.CurrentRoom == null)
		{
			return string.Empty;
		}
		EnvironmentType currentEnvironmentType = DolocAPI.archiveHandle.currentEnvironmentType;
		DayPeriodType currentDayPeriodType = DolocAPI.archiveHandle.CurrentDayPeriodType;
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		if (DolocAPI.IsEventDecoratorComplete("FESTIVAL.EvernightState") && currentEnvironmentType == EnvironmentType.City && currentDayPeriodType == DayPeriodType.Daytime)
		{
			return "PLAY_BGM_21";
		}
		return DolocConfig.Tables.TbBgm.Get(currentEnvironmentType, currentDayPeriodType, currentWeatherType)?.SoundEvent;
	}

	private void PlayBGMInternal(string soundEvent)
	{
		if (soundEvent.IsNullOrEmpty())
		{
			return;
		}
		if (PostSoundEvent(soundEvent, HandleOnBgmEvent))
		{
			isBgmPlaying = true;
			bgmTimeOutTimer.SetInterval(DolocAPI.GlobalParameter.BgmPlayTimeoutDuration);
			currentBgmEventName = soundEvent;
			if (useLog)
			{
				Debug.Log("播放BGM: " + soundEvent);
			}
		}
		else
		{
			Debug.Log("尝试播放BGM失败: " + soundEvent);
		}
	}

	private void HandleOnBgmEvent(object inCookie, AkCallbackType inType, AkCallbackInfo inInfo)
	{
		isBgmPlaying = false;
		bgmTimeOutTimer.Reset();
		currentBgmEventName = string.Empty;
		if (!isBgmPaused)
		{
			RefreshNextBgm(tryDelay: true);
		}
	}

	private void BgmUpdate(float dt)
	{
		if (bgmDelayTimer != null && !isBgmPlaying && bgmDelayTimer.Tick(dt))
		{
			if (useLog)
			{
				Debug.Log("BGM空闲时间到，播放下一首");
			}
			bgmDelayTimer = null;
			RefreshNextBgm(tryDelay: true);
		}
	}

	protected override void __Init()
	{
		base.__Init();
		DolocAPI.OnAfterLoadArchiveData.AddListener(OnDataLoaded);
	}

	public void OnEverythingLoaded()
	{
		DolocAPI.RegisterMsgListener(UserSettingType.VOLUME_GLOBAL, delegate(object _, GameEventArgs args)
		{
			((GameEventArgs<object>)args).TryGetInt(out var value5);
			volumes[SoundBusses.MASTER_AUDIO_BUS] = GlobalVolumeRatio * (float)value5 / 100f;
			ResetVolumes();
		});
		DolocAPI.RegisterMsgListener(UserSettingType.VOLUME_MUSIC, delegate(object _, GameEventArgs args)
		{
			((GameEventArgs<object>)args).TryGetInt(out var value4);
			volumes[SoundBusses.MUSIC] = (float)value4 / 100f;
			SetSoundParameter(SoundParameters.VOLUME_MUSIC, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.MUSIC]);
		});
		DolocAPI.RegisterMsgListener(UserSettingType.VOLUME_SFX_ACTOR, delegate(object _, GameEventArgs args)
		{
			((GameEventArgs<object>)args).TryGetInt(out var value3);
			volumes[SoundBusses.SFX_ACTOR] = (float)value3 / 100f;
			SetSoundParameter(SoundParameters.VOLUME_SFX_ACTOR, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.SFX_ACTOR]);
		});
		DolocAPI.RegisterMsgListener(UserSettingType.VOLUME_SFX_AMBIENT, delegate(object _, GameEventArgs args)
		{
			((GameEventArgs<object>)args).TryGetInt(out var value2);
			volumes[SoundBusses.SFX_AMBIENCE] = (float)value2 / 100f;
			SetSoundParameter(SoundParameters.VOLUME_SFX_AMBIENCE, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.SFX_AMBIENCE]);
		});
		DolocAPI.RegisterMsgListener(UserSettingType.VOLUME_SFX_UI, delegate(object _, GameEventArgs args)
		{
			((GameEventArgs<object>)args).TryGetInt(out var value);
			volumes[SoundBusses.SFX_UI] = (float)value / 100f;
			SetSoundParameter(SoundParameters.VOLUME_SFX_UI, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.SFX_UI]);
		});
		InitVolumes();
	}

	private void InitVolumes()
	{
		volumes[SoundBusses.MASTER_AUDIO_BUS] = GlobalVolumeRatio * (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.VOLUME_GLOBAL) / 100f;
		volumes[SoundBusses.MUSIC] = (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.VOLUME_MUSIC) / 100f;
		volumes[SoundBusses.SFX_ACTOR] = (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.VOLUME_SFX_ACTOR) / 100f;
		volumes[SoundBusses.SFX_AMBIENCE] = (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.VOLUME_SFX_AMBIENT) / 100f;
		volumes[SoundBusses.SFX_UI] = (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.VOLUME_SFX_UI) / 100f;
		ResetVolumes();
	}

	private void ResetVolumes()
	{
		SetSoundParameter(SoundParameters.VOLUME_MUSIC, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.MUSIC]);
		SetSoundParameter(SoundParameters.VOLUME_SFX_ACTOR, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.SFX_ACTOR]);
		SetSoundParameter(SoundParameters.VOLUME_SFX_AMBIENCE, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.SFX_AMBIENCE]);
		SetSoundParameter(SoundParameters.VOLUME_SFX_UI, volumes[SoundBusses.MASTER_AUDIO_BUS] * volumes[SoundBusses.SFX_UI]);
	}

	public void LoadSoundBank(string bankName)
	{
		LoadBankSync(bankName);
	}

	public void LoadSoundBank(SoundBanks bankName)
	{
		LoadSoundBank(bankName.ToString());
	}

	public void LoadSoundBankAsync(string bankName, Action callback = null)
	{
		LoadBankAsync(bankName, callback).Forget();
	}

	public void LoadSoundBankAsync(SoundBanks bankName, Action callback = null)
	{
		LoadSoundBankAsync(bankName.ToString(), callback);
	}

	private void LoadBankSync(string bankName)
	{
		string text = "Assets/GameDatabase/Wwise/Bank/" + bankName + ".asset";
		try
		{
			AsyncOperationHandle<WwiseAddressableSoundBank> handle = Addressables.LoadAssetAsync<WwiseAddressableSoundBank>(text);
			handle.WaitForCompletion();
			AkAddressableBankManager.Instance.LoadBank(handle.Result, decodeBank: false, saveDecodedBank: false, addToBankDictionary: true, loadAsync: false);
			Addressables.Release(handle);
			loadedSoundBanks.Add(bankName);
		}
		catch (Exception ex)
		{
			Debug.LogError("加载Sound Bank失败! address: " + text);
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			throw;
		}
		Debug.Log("已加载Sound Bank: " + bankName);
	}

	private async UniTaskVoid LoadBankAsync(string bankName, Action callback = null)
	{
		try
		{
			LoadSoundBank(bankName);
			callback?.Invoke();
		}
		catch (Exception)
		{
		}
	}

	public void UnloadSoundBank(string bankName)
	{
		loadedSoundBanks.Remove(bankName);
		AkBankManager.UnloadBank(bankName);
		Debug.Log("已卸载Sound Bank: " + bankName);
	}

	public void UnloadSoundBank(SoundBanks bankName)
	{
		UnloadSoundBank(bankName.ToString());
	}

	public void UnloadAllSoundBanks()
	{
		loadedSoundBanks.Clear();
		AkBankManager.UnloadAllBanks();
		Debug.Log("已卸载全部Sound Banks");
	}

	public bool PostSoundEvent(string eventName, AkCallbackManager.EventCallback eventCallback = null, bool ignoreOccupy = false)
	{
		if (eventName.IsNullOrEmpty())
		{
			return false;
		}
		return InternalPostSoundEvent(eventName, null, eventCallback);
	}

	public bool PostSoundEvent(SoundEvents soundEvent, AkCallbackManager.EventCallback eventCallback = null, bool ignoreOccupy = false)
	{
		return InternalPostSoundEvent(soundEvent.ToString(), null, eventCallback);
	}

	public bool PostSoundEvent(string eventName, GameObject emitterObj, AkCallbackManager.EventCallback eventCallback = null, bool ignoreOccupy = false)
	{
		if (eventName.IsNullOrEmpty())
		{
			return false;
		}
		return InternalPostSoundEvent(eventName, emitterObj, eventCallback);
	}

	public bool PostSoundEvent(SoundEvents soundEvent, GameObject emitterObj, AkCallbackManager.EventCallback eventCallback = null, bool ignoreOccupy = false)
	{
		return InternalPostSoundEvent(soundEvent.ToString(), emitterObj, eventCallback);
	}

	public bool Post3DSoundEventOnce(SoundEvents soundEvent, GameObject emitterObj, AkCallbackManager.EventCallback eventCallback = null, bool ignoreOccupy = false)
	{
		if (emitterObj == null)
		{
			return false;
		}
		RegisterGameObject(emitterObj);
		return InternalPostSoundEvent(soundEvent.ToString(), emitterObj, delegate(object cookie, AkCallbackType type, AkCallbackInfo info)
		{
			eventCallback?.Invoke(cookie, type, info);
			UnregisterGameObject(emitterObj);
		});
	}

	private bool InternalPostSoundEvent(string eventName, GameObject emitterObj = null, AkCallbackManager.EventCallback eventCallback = null, bool ignoreOccupy = false)
	{
		eventName = eventName.ToUpper();
		if (!TryOccupySoundGroup(eventName) && !ignoreOccupy)
		{
			if (useLog)
			{
				Debug.Log("(" + ((emitterObj == null) ? "" : emitterObj.name) + "跳过音频事件: " + eventName + ")");
			}
			return false;
		}
		if (useLog)
		{
			Debug.Log(((emitterObj == null) ? "" : emitterObj.name) + "播放音频事件: " + eventName);
		}
		uint in_uFlags = ((eventCallback != null) ? 1u : 0u);
		if (soundEventLUT.TryGetValue(eventName, out var value))
		{
			if (!loadedSoundBanks.Contains(value))
			{
				Debug.LogWarning("触发" + eventName + "事件前未加载对应SoundBank " + value);
				LoadSoundBankAsync(value, delegate
				{
					InternalPostSoundEvent(eventName, emitterObj, eventCallback);
				});
				return true;
			}
		}
		else if (!DolocAPI.gameManager.gameInitConfig.playNoCachedSoundEvent)
		{
			return false;
		}
		try
		{
			if (emitterObj != agentObj && emitterObj != null)
			{
				if (registeredObjs.Contains(emitterObj))
				{
					AkSoundEngine.PostEvent(eventName, emitterObj, in_uFlags, eventCallback, null);
					return true;
				}
				Debug.LogWarning("使用立体声之前请先调用RegisterGameObj()注册发声对象: " + emitterObj.name + "，并适时取消注册。本次音频以全局模式播放。");
			}
			AkSoundEngine.PostEvent(eventName, agentObj, in_uFlags, eventCallback, null);
			return true;
		}
		catch (Exception)
		{
			Debug.LogWarning("播放AK Event: " + eventName + "失败");
			return false;
		}
	}

	public void StopAll()
	{
		StopAmbience();
		StopBgm();
		AkSoundEngine.StopAll();
	}

	public void RegisterSpatialAudioListener(GameObject go)
	{
		if (registeredObjs.Contains(go))
		{
			Debug.LogWarning("聆听对象: " + go.name + " 重复注册");
			return;
		}
		registeredObjs.Add(go);
		AkSoundEngine.RegisterSpatialAudioListener(go);
	}

	public void UnregisterSpatialAudioListener(GameObject go)
	{
		if (!registeredObjs.Contains(go))
		{
			Debug.LogWarning("未注册聆听对象: " + go.name);
			return;
		}
		registeredObjs.Remove(go);
		AkSoundEngine.UnregisterSpatialAudioListener(go);
	}

	public void RegisterGameObject(GameObject go)
	{
		if (!(go == null))
		{
			if (!registeredObjs.Add(go))
			{
				Debug.LogWarning("发声对象: " + go.name + " 重复注册");
			}
			else
			{
				AkSoundEngine.RegisterGameObj(go);
			}
		}
	}

	public void UnregisterGameObject(GameObject go)
	{
		if (!(go == null))
		{
			if (!registeredObjs.Contains(go))
			{
				Debug.LogWarning("未注册发声对象: " + go.name);
				return;
			}
			registeredObjs.Remove(go);
			AkSoundEngine.UnregisterGameObj(go);
		}
	}

	public void UnregisterAllGameObj()
	{
		registeredObjs.Clear();
		AkSoundEngine.UnregisterAllGameObjects();
	}

	private void UpdaterRegisteredObjs(GameObject go)
	{
		AkSoundEngine.SetObjectPosition(go, go.transform);
	}

	private void Update()
	{
		foreach (GameObject registeredObj in registeredObjs)
		{
			UpdaterRegisteredObjs(registeredObj);
		}
		BgmUpdate(Time.deltaTime);
	}

	public void SetSoundParameter(SoundParameters parameter, float value)
	{
		SetSoundParameter(parameter.ToString(), value);
	}

	public void SetSoundParameter(string parameterName, float value)
	{
		AkSoundEngine.SetRTPCValue(parameterName, value);
	}

	public void SetSoundSwitch(string groupName, string switchName, GameObject go)
	{
		if (useLog)
		{
			Debug.Log("set sound switch: " + groupName + " " + switchName);
		}
		if (go == null)
		{
			go = agentObj;
		}
		AkSoundEngine.SetSwitch(groupName, switchName, go);
	}

	public void SetSoundSwitch<T>(T switchType, GameObject go = null) where T : Enum
	{
		SetSoundSwitch(typeof(T).Name, switchType.ToString(), go);
	}

	public void SetSoundState(string groupName, string stateName)
	{
		if (useLog)
		{
			Debug.Log("set sound state: " + groupName + " " + stateName);
		}
		AkSoundEngine.SetState(groupName, stateName);
	}

	public void SetSoundState<T>(T stateType) where T : Enum
	{
		SetSoundState(typeof(T).Name, stateType.ToString());
	}

	public void PostTrigger(SoundTriggers trigger, GameObject go)
	{
		PostTrigger(trigger.ToString(), go);
	}

	public void PostTrigger(string triggerName, GameObject go)
	{
		AkSoundEngine.PostTrigger(triggerName, go);
	}

	private void OnDataLoaded(bool isNewGame)
	{
		RefreshBgmImmediately();
		RefreshAmbience();
	}

	public void OnRoomEntered(Room current)
	{
		bool valueOrDefault = (current?.SceneConfig?.SwitchBgmImmediately).GetValueOrDefault();
		if (bgmSwitchState || valueOrDefault)
		{
			RefreshBgmImmediately();
		}
		else
		{
			RefreshNextBgm(tryDelay: false);
		}
		bgmSwitchState = valueOrDefault;
		RefreshAmbience();
	}

	public void OnWeatherChange(WeatherType weatherType)
	{
		RefreshNextBgm(tryDelay: false);
		RefreshAmbience();
	}

	private bool TryOccupySoundGroup(string eventName)
	{
		if (!exclusiveGroupTable.DataMap.TryGetValue(eventName, out var value))
		{
			return true;
		}
		if (ocupiedGroup.TryGetValue(value.Group, out var value2))
		{
			if (Time.time - value2.time >= sfxGroupInterval)
			{
				ocupiedGroup[value.Group] = new SfxStamp(eventName, Time.time);
				return true;
			}
			return false;
		}
		ocupiedGroup.Add(value.Group, new SfxStamp(eventName, Time.time));
		return true;
	}
}
