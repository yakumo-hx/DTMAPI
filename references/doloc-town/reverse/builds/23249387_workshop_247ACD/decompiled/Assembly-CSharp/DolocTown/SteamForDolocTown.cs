using DolocTown.Config;
using DolocTown.Config.Localization;
using RedSaw.CommandLineInterface;
using Steamworks;
using UnityEngine;

namespace DolocTown;

public class SteamForDolocTown : DolocObject
{
	[CommandProperty("steam")]
	private static SteamForDolocTown instance;

	[SerializeField]
	private bool enableSteamSDK = true;

	public bool isSteamSDKInitialized { get; private set; }

	public bool EnableSteamSDK => enableSteamSDK;

	public string Language
	{
		get
		{
			if (!SteamManager.Initialized)
			{
				return null;
			}
			return SteamApps.GetCurrentGameLanguage();
		}
	}

	public string LanguageForDolocTown
	{
		get
		{
			if (Language == null)
			{
				return "zh-CN";
			}
			foreach (LocalizationInfo data in DolocConfig.Tables.TbLocalization.DataList)
			{
				if (Language == data.SteamL10nId)
				{
					return data.Id;
				}
			}
			return "en";
		}
	}

	private void Start()
	{
		if (SteamInput.Shutdown())
		{
			Debug.Log("SteamInput shutdown success.");
		}
	}

	protected override void __Init()
	{
		base.__Init();
		instance = this;
		if (enableSteamSDK && !isSteamSDKInitialized)
		{
			if (!SteamManager.Initialized)
			{
				SteamAPI.RestartAppIfNecessary(new AppId_t(2285550u));
			}
			if (SteamManager.Initialized)
			{
				isSteamSDKInitialized = true;
				Debug.Log("SteamSDK initialize success.");
			}
			else
			{
				Debug.LogError("SteamSDK initialize failed.");
			}
		}
	}
}
