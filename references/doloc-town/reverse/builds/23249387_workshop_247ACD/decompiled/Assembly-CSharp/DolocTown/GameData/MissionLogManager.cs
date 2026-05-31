using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class MissionLogManager : IDataPersistence
{
	public const string filename = "mission_logs";

	private static MissionLogManager instance;

	[JsonProperty]
	private List<string>[] totalLogs;

	public static string FilePath => Path.Combine(Application.persistentDataPath, "mission_logs");

	public static void AppendLog(string log)
	{
		int archiveIndex = DolocAPI.archiveHandle.archiveIndex;
		instance?.AppendLog(archiveIndex, log);
	}

	public static void LoadMissionLogs()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				instance = JsonConvert.DeserializeObject<MissionLogManager>(File.ReadAllText(FilePath));
			}
			else
			{
				instance = new MissionLogManager();
			}
		}
		catch (Exception ex)
		{
			Debug.LogError($"加载任务日志失败: {ex}");
			Debug.LogException(ex);
			instance = new MissionLogManager();
		}
		if (instance == null)
		{
			instance = new MissionLogManager();
		}
	}

	public static void SaveMissionLogs()
	{
		if (instance == null)
		{
			return;
		}
		try
		{
			string contents = JsonConvert.SerializeObject(instance, Formatting.Indented);
			File.WriteAllText(FilePath, contents);
		}
		catch (Exception ex)
		{
			Debug.LogError($"保存任务日志失败: {ex}");
			Debug.LogException(ex);
		}
	}

	[JsonConstructor]
	public MissionLogManager(List<string>[] totalLogs = null)
	{
		if (totalLogs == null)
		{
			totalLogs = new List<string>[DolocAPI.gameManager.archiveFileCount];
			for (int i = 0; i < totalLogs.Length; i++)
			{
				totalLogs[i] = new List<string>();
			}
			return;
		}
		this.totalLogs = totalLogs;
		if (totalLogs.Length == DolocAPI.gameManager.archiveFileCount)
		{
			return;
		}
		List<string>[] array = new List<string>[DolocAPI.gameManager.archiveFileCount];
		for (int j = 0; j < array.Length; j++)
		{
			if (j < totalLogs.Length)
			{
				array[j] = totalLogs[j] ?? new List<string>();
			}
			else
			{
				array[j] = new List<string>();
			}
		}
		this.totalLogs = array;
	}

	public void AppendLog(int archiveId, string log)
	{
		if (!log.IsNullOrEmpty() && archiveId >= 0 && archiveId < totalLogs.Length)
		{
			totalLogs[archiveId].Add($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]<{Application.version}>: {log}");
		}
	}
}
