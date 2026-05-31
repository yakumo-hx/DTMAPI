using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DolocTown;

public class CopyUpdateLogTest : MonoBehaviour
{
	public string logFilepath => Application.dataPath + "/UpdateLogs/.history/log.json";

	private IEnumerable<string> LogFiles
	{
		get
		{
			string path = Application.dataPath + "/UpdateLogs";
			if (!Directory.Exists(path))
			{
				Debug.LogError("日志文件夹不存在");
				yield break;
			}
			string[] files = Directory.GetFiles(path, "*.txt");
			for (int i = 0; i < files.Length; i++)
			{
				yield return files[i];
			}
		}
	}

	private IEnumerable<string> TotalUnhandledLogs
	{
		get
		{
			foreach (string logFile in LogFiles)
			{
				foreach (string unhandledLogInfo in GetUnhandledLogInfos(logFile))
				{
					yield return unhandledLogInfo;
				}
			}
		}
	}

	private IEnumerable<string> GetUnhandledLogInfos(string filepath)
	{
		string path = Path.GetDirectoryName(filepath) + "/.history/" + Path.GetFileName(filepath) + ".history";
		if (!File.Exists(path))
		{
			IEnumerable<string> enumerable = from line in File.ReadAllLines(filepath)
				where line.Trim().Length > 0
				select line;
			File.WriteAllLines(path, enumerable);
			return enumerable;
		}
		HashSet<string> other = new HashSet<string>(File.ReadAllLines(path));
		HashSet<string> hashSet = new HashSet<string>(File.ReadAllLines(filepath));
		hashSet.ExceptWith(other);
		IEnumerable<string> enumerable2 = from line in hashSet.ToArray()
			where line.Trim().Length > 0
			select line;
		File.AppendAllLines(path, enumerable2);
		return enumerable2;
	}

	private void UpdateLogFile()
	{
		if (!File.Exists(logFilepath))
		{
			File.Create(logFilepath).Close();
		}
		if (JsonUtils.ConvertR<JObject>(File.ReadAllText(logFilepath), out var value))
		{
			value = new JObject();
		}
		string version = Application.version;
		JArray jArray = (JArray)(value.ContainsKey(version) ? (value[version] as JArray) : (value[version] = new JArray()));
		foreach (string totalUnhandledLog in TotalUnhandledLogs)
		{
			jArray.Add(totalUnhandledLog);
		}
		string contents = value.ToString();
		File.WriteAllText(logFilepath, contents);
	}

	private void ShowLogFilePathes()
	{
		foreach (string logFile in LogFiles)
		{
			Debug.Log(logFile);
		}
	}

	private void ShowUnhandledLogInfos()
	{
		foreach (string logFile in LogFiles)
		{
			foreach (string unhandledLogInfo in GetUnhandledLogInfos(logFile))
			{
				Debug.Log(unhandledLogInfo);
			}
		}
	}

	private void ShowUsername()
	{
		Debug.Log(Environment.UserName);
	}
}
