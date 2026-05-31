using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "GameOuterConfig", menuName = "多洛可小镇/配置/游戏外部配置")]
public class GameOuterConfigSO : ScriptableObject
{
	[SerializeField]
	public bool enableGameConsole;

	[SerializeField]
	public bool disableSteamValidator;

	[SerializeField]
	public bool ignoreMinimumVersion;

	[SerializeField]
	public bool allowFastTravelMenu;

	public GameOuterConfig GetGameOuterConfig()
	{
		return new GameOuterConfig(enableGameConsole, disableSteamValidator, ignoreMinimumVersion, allowFastTravelMenu);
	}
}
