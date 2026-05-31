using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbGameKeyIcon
{
	private readonly List<GameKeyIconInfo> _dataList;

	private Dictionary<(string, string), GameKeyIconInfo> _dataMapUnion;

	private Dictionary<string, HashSet<string>> pathCache = new Dictionary<string, HashSet<string>>();

	public List<GameKeyIconInfo> DataList => _dataList;

	public TbGameKeyIcon(JSONNode _json)
	{
		_dataList = new List<GameKeyIconInfo>();
		foreach (JSONNode child in _json.Children)
		{
			GameKeyIconInfo item = GameKeyIconInfo.DeserializeGameKeyIconInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(string, string), GameKeyIconInfo>();
		List<GameKeyIconInfo> list = new List<GameKeyIconInfo>();
		foreach (GameKeyIconInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.DeviceName, data.Path), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.DeviceName, data.Path)} in table: TbGameKeyIcon");
			}
		}
		foreach (GameKeyIconInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public GameKeyIconInfo Get(string device_name, string path)
	{
		if (!_dataMapUnion.TryGetValue((device_name, path), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (GameKeyIconInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (GameKeyIconInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		foreach (GameKeyIconInfo data in DataList)
		{
			pathCache.TryAdd(data.DeviceName, new HashSet<string>());
			pathCache[data.DeviceName].Add(data.DevicePath + "/" + data.Path);
		}
	}

	public HashSet<string> GetPathsByDevice(DolocInputDeviceType deviceType)
	{
		pathCache.TryGetValue(deviceType.ToString(), out var value);
		return value;
	}
}
