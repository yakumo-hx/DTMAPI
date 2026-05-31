using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbL10nImage
{
	private readonly List<L10nImageInfo> _dataList;

	private Dictionary<(string, string), L10nImageInfo> _dataMapUnion;

	public List<L10nImageInfo> DataList => _dataList;

	public TbL10nImage(JSONNode _json)
	{
		_dataList = new List<L10nImageInfo>();
		foreach (JSONNode child in _json.Children)
		{
			L10nImageInfo item = L10nImageInfo.DeserializeL10nImageInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(string, string), L10nImageInfo>();
		List<L10nImageInfo> list = new List<L10nImageInfo>();
		foreach (L10nImageInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.ImgId, data.LanguageId), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.ImgId, data.LanguageId)} in table: TbL10nImage");
			}
		}
		foreach (L10nImageInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public L10nImageInfo Get(string img_id, string language_id)
	{
		if (!_dataMapUnion.TryGetValue((img_id, language_id), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (L10nImageInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (L10nImageInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
