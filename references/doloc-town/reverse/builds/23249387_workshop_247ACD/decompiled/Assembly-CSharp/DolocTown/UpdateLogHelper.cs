using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DolocTown;

public class UpdateLogHelper
{
	private enum DevLogType
	{
		Add,
		Chg,
		Rem,
		Fix,
		Opt,
		Rfc,
		Rsm,
		Info,
		Unknown
	}

	private struct DevLog
	{
		public DevLogType logType;

		public string content;
	}

	private class DevLogVersion
	{
		public readonly string version;

		public readonly bool isDefaultVersion;

		public readonly List<DevLog> allDates = new List<DevLog>();

		public DevLogVersion(string version, bool isDefaultVersion = false)
		{
			this.version = version;
			this.isDefaultVersion = isDefaultVersion;
		}

		public void AppendLog(DevLog log)
		{
			allDates.Add(log);
		}
	}

	private class LogInfos
	{
		public readonly Dictionary<string, DevLogVersion> allVersions = new Dictionary<string, DevLogVersion>();

		private readonly DevLogVersion mostRecentVersion;

		private DevLogVersion currentVersion;

		public LogInfos()
		{
			mostRecentVersion = new DevLogVersion("__current_version__", isDefaultVersion: true);
			allVersions.Add(mostRecentVersion.version, mostRecentVersion);
		}

		public void AddVersion(string version)
		{
			if (!allVersions.TryGetValue(version, out currentVersion))
			{
				allVersions.Add(version, new DevLogVersion(version));
				currentVersion = allVersions[version];
			}
		}

		public void AppendLog(DevLog log)
		{
			if (currentVersion == null)
			{
				mostRecentVersion.AppendLog(log);
			}
			else
			{
				currentVersion.AppendLog(log);
			}
		}

		public void Reset()
		{
			currentVersion = mostRecentVersion;
		}
	}

	private static string _updateLogs;

	private readonly List<Func<string, bool, string>> compileFunctions = new List<Func<string, bool, string>> { Added, Changed, Removed, Fixed, Optimized, Refractor, Resume, Information, Unknown };

	private readonly LogInfos _logInfos = new LogInfos();

	private static UpdateLogHelper instance => new UpdateLogHelper();

	private static string UpdateLogFolder => Path.Join(Directory.GetParent(Application.dataPath).FullName, "UpdateLogs");

	private static string MainVersion
	{
		get
		{
			string version = Application.version;
			string[] array = version.Split('.');
			if (array.Length < 2)
			{
				return version;
			}
			return array[0] + "." + array[1];
		}
	}

	public static string GetUpdateLog(bool onlyMainVersion = true, bool colored = true)
	{
		if (_updateLogs == null)
		{
			_updateLogs = instance.LoadLogs(onlyMainVersion, colored);
		}
		return _updateLogs;
	}

	private string LoadLogs(bool onlyMainVersion = true, bool colored = true)
	{
		foreach (string item in GetAllFileInDirectory(UpdateLogFolder))
		{
			Debug.Log("解析日志文件:" + item);
			ParseFile(File.ReadAllLines(item));
		}
		StringBuilder stringBuilder = new StringBuilder();
		string mainVersion = MainVersion;
		foreach (DevLogVersion item2 in SortLogVersions(_logInfos.allVersions.Values))
		{
			if (onlyMainVersion && !item2.version.StartsWith(mainVersion))
			{
				continue;
			}
			if (!item2.isDefaultVersion)
			{
				stringBuilder.Append(item2.version + "\n");
				stringBuilder.Append("    \n");
			}
			foreach (DevLog allDate in item2.allDates)
			{
				stringBuilder.Append(Compile(allDate, colored));
			}
			stringBuilder.Append("    \n");
		}
		return stringBuilder.ToString();
	}

	private static IEnumerable<string> GetAllFileInDirectory(string path)
	{
		if (!Directory.Exists(path))
		{
			yield break;
		}
		string[] files = Directory.GetFiles(path);
		foreach (string text in files)
		{
			if (text.EndsWith(".txt"))
			{
				yield return text;
			}
		}
	}

	private void ParseFile(string[] lines)
	{
		if (lines.IsNullOrEmpty())
		{
			return;
		}
		_logInfos.Reset();
		for (int i = 0; i < lines.Length; i++)
		{
			string text = lines[i].Trim();
			if (!text.IsNullOrEmpty() && !text.StartsWith("//"))
			{
				DevLog result;
				if (text.StartsWith("[version]"))
				{
					_logInfos.AddVersion(text.Replace("[version]", "").Trim());
				}
				else if (ParseLog(text, out result))
				{
					_logInfos.AppendLog(result);
				}
			}
		}
	}

	private static bool ParseLog(string log, out DevLog result)
	{
		result = default(DevLog);
		Match match = Regex.Match(log, "\\[([^\\]]+)\\](.*)");
		if (!match.Success)
		{
			return false;
		}
		string value = match.Groups[1].Value.Trim();
		string content = match.Groups[2].Value.Trim();
		if (!Enum.TryParse<DevLogType>(value, ignoreCase: true, out var result2))
		{
			return false;
		}
		result = new DevLog
		{
			logType = result2,
			content = content
		};
		return true;
	}

	private string Compile(DevLog log, bool colored)
	{
		return compileFunctions[(int)log.logType](log.content, colored);
	}

	private static string Added(string msg, bool colored)
	{
		if (!colored)
		{
			return "[新增] " + msg + "\n";
		}
		return "<color=#92e8c0>[新增] " + msg + "</color>\n";
	}

	private static string Changed(string msg, bool colored)
	{
		if (!colored)
		{
			return "[调整] " + msg + "\n";
		}
		return "<color=#fffde3>[调整] " + msg + "</color>\n";
	}

	private static string Removed(string msg, bool colored)
	{
		if (!colored)
		{
			return "[移除] " + msg + "\n";
		}
		return "<color=#b13c45>[移除] " + msg + "</color>\n";
	}

	private static string Fixed(string msg, bool colored)
	{
		if (!colored)
		{
			return "[修复] " + msg + "\n";
		}
		return "<color=#9bd547>[修复] " + msg + "</color>\n";
	}

	private static string Optimized(string msg, bool colored)
	{
		if (!colored)
		{
			return "[优化] " + msg + "\n";
		}
		return "<color=#ff7f4f>[优化] " + msg + "</color>\n";
	}

	private static string Refractor(string msg, bool colored)
	{
		if (!colored)
		{
			return "[重构] " + msg + "\n";
		}
		return "<color=#df426e>[重构] " + msg + "</color>\n";
	}

	private static string Resume(string msg, bool colored)
	{
		if (!colored)
		{
			return "[重构] " + msg + "\n";
		}
		return "<color=#f2ff4f>[恢复] " + msg + "</color>\n";
	}

	private static string Information(string msg, bool colored)
	{
		if (!colored)
		{
			return msg + "\n";
		}
		return "<color=#fffde3>" + msg + "</color>\n";
	}

	private static string Unknown(string msg, bool colored)
	{
		if (!colored)
		{
			return "[未知] " + msg + "\n";
		}
		return "<color=#fffde3>[未知] " + msg + "</color>\n";
	}

	private static IEnumerable<DevLogVersion> SortLogVersions(IEnumerable<DevLogVersion> versions)
	{
		List<DevLogVersion> list = versions.ToList();
		if (list.IsNullOrEmpty())
		{
			return Array.Empty<DevLogVersion>();
		}
		list.Sort((DevLogVersion v1, DevLogVersion v2) => -TryCompareVersion(v1.version, v2.version));
		return list;
	}

	private static int TryCompareVersion(string v1, string v2)
	{
		if (v1 == v2)
		{
			return 0;
		}
		try
		{
			int num = v1.LastIndexOf('.');
			float num2 = float.Parse(v1[..num]);
			int num3 = int.Parse(v1[new Range(end: v1.Length, start: num + 1)]);
			int num4 = v2.LastIndexOf('.');
			float num5 = float.Parse(v2[..num4]);
			int num6 = int.Parse(v2[new Range(end: v2.Length, start: num4 + 1)]);
			if (num2 > num5)
			{
				return 1;
			}
			if (num2 < num5)
			{
				return -1;
			}
			if (num3 > num6)
			{
				return 1;
			}
			if (num3 < num6)
			{
				return -1;
			}
		}
		catch (Exception)
		{
			return -1;
		}
		return 0;
	}
}
