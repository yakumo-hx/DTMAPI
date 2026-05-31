using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbL10nLink
{
	private readonly List<L10nLinkInfo> _dataList;

	private Dictionary<(string, string), L10nLinkInfo> _dataMapUnion;

	public List<L10nLinkInfo> DataList => _dataList;

	public TbL10nLink(JSONNode _json)
	{
		_dataList = new List<L10nLinkInfo>();
		foreach (JSONNode child in _json.Children)
		{
			L10nLinkInfo item = L10nLinkInfo.DeserializeL10nLinkInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(string, string), L10nLinkInfo>();
		List<L10nLinkInfo> list = new List<L10nLinkInfo>();
		foreach (L10nLinkInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.LinkId, data.LanguageId), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.LinkId, data.LanguageId)} in table: TbL10nLink");
			}
		}
		foreach (L10nLinkInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public L10nLinkInfo Get(string link_id, string language_id)
	{
		if (!_dataMapUnion.TryGetValue((link_id, language_id), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (L10nLinkInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (L10nLinkInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
