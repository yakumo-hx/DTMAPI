using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DolocTown;

public class VersionPatch
{
	public readonly Version version;

	public readonly List<(string, MethodInfo)> patchFunctions;

	public string Version => $"{version.Major}.{version.Minor}.{version.Build}";

	public bool IsEmpty => patchFunctions.Count == 0;

	public VersionPatch(Version version, List<(string, MethodInfo)> patchFunctions)
	{
		this.version = version;
		this.patchFunctions = patchFunctions;
	}

	public void ApplyPatches(Action<string> logFunction = null, Action<Exception> exceptionHandle = null)
	{
		logFunction?.Invoke("<color=#ffff00>应用补丁集: " + Version + "</color>");
		foreach (var patchFunction in patchFunctions)
		{
			string item = patchFunction.Item1;
			MethodInfo item2 = patchFunction.Item2;
			string text = (item.IsNullOrEmpty() ? item2.Name : item);
			try
			{
				item2.Invoke(null, null);
				logFunction?.Invoke("<color=#00ff00>应用补丁函数成功!:\"" + text + "\"</color>");
			}
			catch (Exception ex)
			{
				logFunction?.Invoke("<color=#ff0000>补丁\"" + text + "\"应用失败..\"" + ex.Message + "\"</color>");
				exceptionHandle?.Invoke(ex);
			}
		}
	}

	public static bool operator >(VersionPatch a, VersionPatch b)
	{
		if (a == null || b == null)
		{
			throw new ArgumentNullException();
		}
		return a.version.CompareTo(b.version) > 0;
	}

	public static bool operator <(VersionPatch a, VersionPatch b)
	{
		if (a == null || b == null)
		{
			throw new ArgumentNullException();
		}
		return a.version.CompareTo(b.version) < 0;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder("version <" + Version + ">");
		for (int i = 0; i < patchFunctions.Count; i++)
		{
			string text = (patchFunctions[i].Item1.IsNullOrEmpty() ? patchFunctions[i].Item2.Name : patchFunctions[i].Item1);
			stringBuilder.Append("\n\t" + text);
		}
		return stringBuilder.ToString();
	}
}
