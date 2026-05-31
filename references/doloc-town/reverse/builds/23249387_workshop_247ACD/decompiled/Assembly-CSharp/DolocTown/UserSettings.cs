using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Settings;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class UserSettings
{
	private Dictionary<UserSettingType, object> currentData = new Dictionary<UserSettingType, object>();

	public bool autoAim { get; private set; }

	public bool useBatterMode { get; private set; }

	public bool joystickShootByDir { get; private set; }

	public bool useScreenShake { get; private set; }

	public bool cameraSmoothMove { get; private set; }

	public bool autoUseBox { get; private set; }

	public bool usePortalAnimation { get; private set; }

	public bool allowCollectData { get; private set; }

	public float fishingGameSpeedMultiplier { get; private set; }

	public float fishingGameDelayTime { get; private set; }

	public InputSchemaType inputSchemaType { get; private set; }

	public bool useTextShakeEffect { get; private set; }

	public bool operationTextBlod { get; private set; }

	public bool enableBackpackShortcut { get; private set; }

	public bool allowEnterRoomInAir { get; private set; }

	public bool showSortLockIcons { get; private set; }

	public bool useQuickCraft { get; private set; }

	public bool openMenuWhenRecipeConflict { get; private set; }

	public bool selectFirstRecipe { get; private set; }

	public bool saveOnNap { get; private set; }

	public float sunIntensityAdder { get; private set; }

	private TbUserSetting tb => DolocConfig.Tables.TbUserSetting;

	[JsonProperty]
	private Dictionary<string, object> settings => currentData.ToDictionary((KeyValuePair<UserSettingType, object> kv) => kv.Key.ToString(), (KeyValuePair<UserSettingType, object> kv) => kv.Value);

	public void OnEverythingLoaded()
	{
		DolocAPI.RegisterMsgListener(UserSettingType.LANGUAGE_TEXT, delegate(object _, GameEventArgs args)
		{
			DolocAPI.SwitchLanguage((string)((GameEventArgs<object>)args).value);
		});
		DolocAPI.ppm.RefreshResidentFilter();
	}

	public void UpdateCachedData()
	{
		autoAim = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_AUTO_AIM);
		useBatterMode = false;
		string orDefault = DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.OPERATION_SHOOTING_MODE);
		joystickShootByDir = orDefault == "dir";
		useScreenShake = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.GAME_USE_SCREEN_SHAKE);
		cameraSmoothMove = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.GAME_USE_CAMERA_SMOOTH_MOVE);
		autoUseBox = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_AUTO_USE_BOX);
		usePortalAnimation = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.GAME_USE_PORTAL_ANIMATION);
		allowCollectData = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR);
		useTextShakeEffect = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.TEXT_SHAKE_EFFECT);
		operationTextBlod = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.TEXT_INTERACTION_PROMPT_BLOD);
		enableBackpackShortcut = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.INPUT_ENABLE_BACKPACK_SHORTCUT);
		allowEnterRoomInAir = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_ALLOW_ENTER_ROOM_IN_AIR);
		showSortLockIcons = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_SHOW_SORT_LOCK_ICONS);
		int orDefault2 = DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.FISHING_NOTE_SPEED_LEVEL);
		float value = 1f - DolocAPI.GlobalParameter.FishingNoteSpeedDecreasePerLevel * (float)(5 - orDefault2);
		fishingGameSpeedMultiplier = Mathf.Clamp01(value);
		fishingGameDelayTime = Mathf.Max(0f, (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.FISHING_PREPARATION_TIME) * DolocAPI.GlobalParameter.FishingGamePreparationTimeScale);
		useQuickCraft = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_ALLOW_QUICK_SYNTHESIS);
		string orDefault3 = DolocAPI.userSettings.GetOrDefault<string>(UserSettingType.OPERATION_QUICK_SYNTHESIS_CONFLICT);
		openMenuWhenRecipeConflict = orDefault3 == "none";
		selectFirstRecipe = orDefault3 == "first";
		saveOnNap = DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OPERATION_SAVE_ON_NAP);
		sunIntensityAdder = (float)DolocAPI.userSettings.GetOrDefault<int>(UserSettingType.GRAPHICS_SUN_INTENSITY) * DolocAPI.GlobalParameter.SunLightIntensityAdderFactor;
	}

	[JsonConstructor]
	public UserSettings(Dictionary<string, object> settings = null)
	{
		if (settings != null)
		{
			foreach (var (value, obj2) in settings)
			{
				if (Enum.TryParse<UserSettingType>(value, out var result) && tb.DataMap.ContainsKey(result) && obj2 != null)
				{
					currentData[result] = obj2;
				}
			}
		}
		foreach (UserSettingInfo data in tb.DataList)
		{
			if (!currentData.ContainsKey(data.Id))
			{
				object defaultValue = GetDefaultValue(data);
				if (defaultValue != null)
				{
					currentData.Add(data.Id, defaultValue);
				}
			}
		}
	}

	public UserSettings Copy()
	{
		return new UserSettings(settings);
	}

	public bool Equals(UserSettings otherSettings)
	{
		foreach (UserSettingInfo data in tb.DataList)
		{
			UserSettingType id = data.Id;
			if (currentData.TryGetValue(id, out var value) && otherSettings.currentData.TryGetValue(id, out var value2) && !value.Equals(value2))
			{
				return false;
			}
		}
		return true;
	}

	private object GetDefaultValue(UserSettingInfo item)
	{
		SettingComponentBase component = item.Component;
		if (!(component is ToggleSettingComponent toggleSettingComponent))
		{
			if (!(component is SliderSettingComponent sliderSettingComponent))
			{
				if (component is OptionSettingComponent optionSettingComponent)
				{
					return optionSettingComponent.DefaultValue;
				}
				return null;
			}
			return sliderSettingComponent.DefaultValue;
		}
		return toggleSettingComponent.DefaultValue;
	}

	private object GetDefaultValue(UserSettingType id)
	{
		if (!tb.DataMap.TryGetValue(id, out var value))
		{
			Debug.LogError($"没有id为<{id}>的设置项");
			return null;
		}
		return GetDefaultValue(value);
	}

	private T GetDefaultValue<T>(UserSettingType id)
	{
		object defaultValue = GetDefaultValue(id);
		return ConvertType<T>(defaultValue, id);
	}

	private T ConvertType<T>(object obj, UserSettingType id)
	{
		try
		{
			return (T)Convert.ChangeType(obj, typeof(T));
		}
		catch (Exception message)
		{
			Debug.LogError($"设置项<{id}>的值为{obj.GetType()}:{obj}, 与{typeof(T)}类型不符合");
			Debug.LogError(message);
			return default(T);
		}
	}

	public T GetOrDefault<T>(UserSettingType id)
	{
		if (!currentData.TryGetValue(id, out var value))
		{
			return GetDefaultValue<T>(id);
		}
		return ConvertType<T>(value, id);
	}

	public object GetOrDefault(UserSettingType id)
	{
		if (!currentData.TryGetValue(id, out var value))
		{
			return GetDefaultValue(id);
		}
		return value;
	}

	public void SetValue(UserSettingType id, object value, bool sendMessage = true)
	{
		if (!currentData.TryGetValue(id, out var value2))
		{
			value2 = GetDefaultValue(id);
		}
		currentData[id] = value;
		if (!value.Equals(value2))
		{
			Debug.Log($"设置项<{id}>从<{value2}>变更为<{value}>");
			if (sendMessage)
			{
				DolocAPI.Broadcast(id, new GameEventArgs<object>(value));
			}
		}
	}

	public void Reset()
	{
		Revert(new UserSettings());
	}

	public void Revert(UserSettings settings)
	{
		if (settings == null)
		{
			settings = new UserSettings();
		}
		foreach (KeyValuePair<UserSettingType, object> currentDatum in settings.currentData)
		{
			SetValue(currentDatum.Key, currentDatum.Value);
		}
	}

	public void RevertToDefaultByGroup(string groupId)
	{
		UserSettingType[] array = currentData.Keys.ToArray();
		foreach (UserSettingType userSettingType in array)
		{
			if (tb.DataMap.TryGetValue(userSettingType, out var value) && !(value.Group != groupId))
			{
				SetValue(userSettingType, GetDefaultValue(userSettingType));
			}
		}
	}
}
