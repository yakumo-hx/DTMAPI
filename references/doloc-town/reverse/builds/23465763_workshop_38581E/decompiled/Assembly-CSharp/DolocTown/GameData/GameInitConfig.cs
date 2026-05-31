using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;

namespace DolocTown.GameData;

[CreateAssetMenu(fileName = "GameInitConfig", menuName = "多洛可小镇/配置/游戏初始化配置")]
public class GameInitConfig : SerializedScriptableObject
{
	[Serializable]
	public struct Item
	{
		[SerializeField]
		public string name;

		[SerializeField]
		public ushort count;
	}

	private struct PortalInfo
	{
		[SerializeField]
		public string id;
	}

	[Serializable]
	public struct LuaScript
	{
		public bool toggle;

		public TextAsset script;

		public void Execute()
		{
			if (!(script == null))
			{
				string text = script.text;
				if (text != null && text.Length > 0)
				{
					DolocAPI.RunLua(script.text);
				}
			}
		}
	}

	[Serializable]
	public struct VersionCommandScriptGroup
	{
		public string version;

		public CommandScript[] appendScripts;

		public void Execute(string dataVersion)
		{
			Version obj = new Version(dataVersion);
			Version value = new Version(version);
			if (obj.CompareTo(value) < 0)
			{
				CommandScript[] array = appendScripts;
				foreach (CommandScript commandScript in array)
				{
					commandScript.Execute();
				}
			}
		}
	}

	[Serializable]
	public struct CommandScript
	{
		public bool toggle;

		public TextAsset script;

		public void Execute()
		{
			if (!toggle || script == null)
			{
				return;
			}
			string[] array = script.text.Split("\n");
			foreach (string text in array)
			{
				if (IsCommand(text))
				{
					DolocAPI.ExecuteCommand(text, out var _);
				}
			}
		}

		private bool IsCommand(string command)
		{
			if (!string.IsNullOrEmpty(command))
			{
				return !command.Trim().StartsWith("-");
			}
			return false;
		}
	}

	[Serializable]
	public struct DLCScript
	{
		[SerializeField]
		public bool toggle;

		[SerializeField]
		[Tooltip("该版本发生变化时，会重新执行该脚本")]
		public string version;

		[SerializeField]
		public TextAsset script;

		public void Execute()
		{
			if (script == null || script.text.IsNullOrEmpty())
			{
				return;
			}
			string[] array = script.text.Split('\n');
			foreach (string text in array)
			{
				if (IsCommand(text))
				{
					DolocAPI.ExecuteCommand(text, out var _);
				}
			}
		}

		private bool IsCommand(string command)
		{
			if (!string.IsNullOrEmpty(command))
			{
				return !command.Trim().StartsWith("-");
			}
			return false;
		}
	}

	[SerializeField]
	public bool hideVersionInfo;

	[SerializeField]
	public bool skipArchiveData;

	[SerializeField]
	public bool useDefaultArchive;

	[SerializeField]
	public bool skipInitAnim;

	[SerializeField]
	public bool skipTraining;

	[SerializeField]
	public bool ignoreMaterialCost;

	[SerializeField]
	public bool skipMoneyVerifyInShop;

	[SerializeField]
	public bool agentInvincible;

	[SerializeField]
	public bool resetUserSettings;

	[SerializeField]
	public bool ignoreSpiritCost;

	[SerializeField]
	public bool buildingInvincible;

	[SerializeField]
	public bool skipInteractionConditionCheck;

	[SerializeField]
	public bool ignoreBuildingLinkGateUnlock;

	[SerializeField]
	public bool shouldCoverMoveAbilityUnlock;

	[SerializeField]
	public bool showAllWeatherReport;

	[SerializeField]
	public bool skipStartDialogue;

	[SerializeField]
	public bool playNoCachedSoundEvent;

	[SerializeField]
	public bool hideGuidanceTips;

	[SerializeField]
	public bool disableSteamValidator;

	[SerializeField]
	public bool skipFishingGame;

	[SerializeField]
	public bool skipFishingWait;

	[SerializeField]
	public bool forceRollFish;

	[SerializeField]
	public bool ignoreAnimalMoodWhenProduce;

	[SerializeField]
	public bool showAllMissionAppendDescription;

	[SerializeField]
	public bool ignorePlantLimit;

	[SerializeField]
	public bool collectExceptionInDebugMode;

	[SerializeField]
	public bool ignoreCropGeneUnlock;

	[SerializeField]
	public bool ignoreStateChangeMsg;

	[SerializeField]
	public bool ignoreDialogueCommandDebugInfo;

	[SerializeField]
	public bool enableWwiseLog;

	[SerializeField]
	public bool ignoreTextMapperLog;

	[SerializeField]
	public bool showEventMessageLog;

	[SerializeField]
	public bool useLocalBugCollector;

	[SerializeField]
	private PortalInfo[] portals = Array.Empty<PortalInfo>();

	[SerializeField]
	public CommandScript[] commandScripts;

	[SerializeField]
	public VersionCommandScriptGroup[] appendCommandScripts;

	[SerializeField]
	public CommandScript[] patchScripts;

	[SerializeField]
	public DLCScript[] dlcScripts;

	[SerializeField]
	public string initMarkPoint;

	[SerializeField]
	public string afterFaintMarkPoint;

	[SerializeField]
	public YarnProject yarnProject;

	[SerializeField]
	public string openingAnimationNode;

	public bool loadDataUnsafe => false;

	public void InitNewGame(ArchiveDataHandle data)
	{
		data.CurrentMoney = DolocAPI.GlobalParameter.Money;
		data.timeData.InitTotalSeconds(DolocAPI.GlobalParameter.InitTime);
		data.SetWeather(DolocAPI.GlobalParameter.InitWeatherType, shouldRender: true);
	}

	public void InitCommandScripts()
	{
		CommandScript[] array = commandScripts;
		foreach (CommandScript commandScript in array)
		{
			commandScript.Execute();
		}
	}

	public void RunPatchCommandScripts()
	{
		CommandScript[] array = patchScripts;
		foreach (CommandScript commandScript in array)
		{
			commandScript.Execute();
		}
	}

	public void RunVersionAppendCommandScripts(string version)
	{
		VersionCommandScriptGroup[] array = appendCommandScripts;
		foreach (VersionCommandScriptGroup versionCommandScriptGroup in array)
		{
			versionCommandScriptGroup.Execute(version);
		}
	}

	public void EnterTransitionMenu()
	{
		DolocAPI.ShowSmallTextMenu(portals.Select((PortalInfo x) => x.id).ToArray(), new Vector2(50f, 50f), delegate(string x)
		{
			DolocAPI.DoTransport(x);
		});
	}

	public void ExecuteDLCScripts()
	{
		if (!SteamManager.Initialized)
		{
			return;
		}
		DLCScript[] array = dlcScripts;
		for (int i = 0; i < array.Length; i++)
		{
			DLCScript dLCScript = array[i];
			if (!dLCScript.toggle || dLCScript.script == null)
			{
				continue;
			}
			uint result;
			if (!dLCScript.version.TryParseVersion(out var version))
			{
				Debug.LogError("<color=#05ffc5>dlc脚本版本配置异常</color>");
			}
			else if (!uint.TryParse(dLCScript.script.name, out result))
			{
				Debug.LogError("<color=#05ffc5>dlc脚本" + dLCScript.script.name + "不是合法的DLC ID</color>");
			}
			else
			{
				if (!SteamHelper.IsDLCUnlocked(result))
				{
					continue;
				}
				if (DolocAPI.archiveHandle.extraData.steamDLCRecords.TryGetValue(result, out var value))
				{
					if (version.CompareTo(value.gameVersion) > 0)
					{
						Debug.Log(string.Format("<color={0}>检测到DLC\"{1}\"内容更新: 执行命令脚本</color>", "#05ffc5", result));
						dLCScript.Execute();
						DolocAPI.archiveHandle.RecordDLC(result, version, dLCScript.script.text);
					}
				}
				else
				{
					Debug.Log(string.Format("<color={0}>检测到DLC\"{1}\"已安装: 执行命令脚本</color>", "#05ffc5", result));
					dLCScript.Execute();
					DolocAPI.archiveHandle.RecordDLC(result, version, dLCScript.script.text);
				}
			}
		}
	}
}
