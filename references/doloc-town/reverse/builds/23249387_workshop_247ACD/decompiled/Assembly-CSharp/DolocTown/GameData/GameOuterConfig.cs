using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DolocTown.GameData;

public class GameOuterConfig
{
	public readonly bool enableGameConsole;

	public readonly bool disableSteamValidator;

	public readonly bool ignoreMinimumVersion;

	public readonly bool allowFastTravelMenu;

	public static GameOuterConfig LoadFromJson(string json, GameOuterConfigSO defaultConfig)
	{
		if (JsonUtils.ConvertR<JObject>(json, out var value))
		{
			Debug.LogError("无效的外部配置项");
			return defaultConfig.GetGameOuterConfig();
		}
		bool flag = defaultConfig.enableGameConsole;
		if (JsonUtils.GetBool(value, "enableGameConsole", out var value2))
		{
			flag = value2;
		}
		bool flag2 = defaultConfig.disableSteamValidator;
		if (JsonUtils.GetBool(value, "disableSteamValidator", out var value3))
		{
			flag2 = value3;
		}
		bool flag3 = defaultConfig.ignoreMinimumVersion;
		if (JsonUtils.GetBool(value, "ignoreMinimumVersion", out var value4))
		{
			flag3 = value4;
		}
		bool flag4 = defaultConfig.allowFastTravelMenu;
		if (JsonUtils.GetBool(value, "enableTravelMenu", out var value5))
		{
			flag4 = value5;
		}
		return new GameOuterConfig(flag, flag2, flag3, flag4);
	}

	public GameOuterConfig(bool enableGameConsole, bool disableSteamValidator, bool ignoreMinimumVersion, bool allowFastTravelMenu)
	{
		this.enableGameConsole = enableGameConsole;
		this.disableSteamValidator = disableSteamValidator;
		this.ignoreMinimumVersion = ignoreMinimumVersion;
		this.allowFastTravelMenu = allowFastTravelMenu;
	}
}
