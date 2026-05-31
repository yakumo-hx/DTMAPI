using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

public class FileUtils
{
	public static bool TryLoadJson<T>(string path, JsonSerializerSettings settings, out T data) where T : new()
	{
		data = default(T);
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			string value = File.ReadAllText(path);
			data = JsonConvert.DeserializeObject<T>(value, settings);
			if (data == null)
			{
				data = new T();
			}
			Debug.Log("读取文件成功: " + path);
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("读取文件失败: " + path);
			Debug.LogException(exception);
			data = new T();
			return false;
		}
	}

	public static bool TrySaveJson<T>(string path, T data, JsonSerializerSettings settings = null)
	{
		if (settings == null)
		{
			settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};
		}
		try
		{
			string path2 = Path.GetDirectoryName(path) ?? string.Empty;
			if (!Directory.Exists(path2))
			{
				Directory.CreateDirectory(path2);
			}
			string contents = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
			File.WriteAllText(path, contents);
			Debug.Log("写入文件成功: " + path);
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("写入文件失败: " + path);
			Debug.LogException(exception);
			return false;
		}
	}

	public static bool TryLoadText(string path, out string text)
	{
		text = string.Empty;
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			text = File.ReadAllText(path);
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("读取文本失败: " + path);
			Debug.LogException(exception);
			return false;
		}
	}

	public static bool TrySaveText(string path, string text)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
			File.WriteAllText(path, text);
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("写入文本失败: " + path);
			Debug.LogException(exception);
			return false;
		}
	}

	public static List<string> GetFilesExcludingDirs(string rootPath, string searchPattern, params string[] ignoreDirNames)
	{
		List<string> list = new List<string>();
		if (!Directory.Exists(rootPath))
		{
			return list;
		}
		HashSet<string> ignoreSet = new HashSet<string>(ignoreDirNames, StringComparer.OrdinalIgnoreCase);
		Traverse(rootPath, searchPattern, ignoreSet, list);
		return list;
	}

	private static void Traverse(string currentDir, string searchPattern, HashSet<string> ignoreSet, List<string> results)
	{
		string fileName = Path.GetFileName(currentDir);
		if (ignoreSet.Contains(fileName))
		{
			return;
		}
		try
		{
			results.AddRange(Directory.GetFiles(currentDir, searchPattern, SearchOption.TopDirectoryOnly));
		}
		catch
		{
		}
		try
		{
			string[] directories = Directory.GetDirectories(currentDir);
			for (int i = 0; i < directories.Length; i++)
			{
				Traverse(directories[i], searchPattern, ignoreSet, results);
			}
		}
		catch
		{
		}
	}
}
