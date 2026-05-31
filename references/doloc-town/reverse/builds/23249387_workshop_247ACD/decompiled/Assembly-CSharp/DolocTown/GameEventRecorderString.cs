using System.Collections.Generic;
using System.Text;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

public class GameEventRecorderString : GameEventRecorder
{
	[JsonProperty]
	private readonly Dictionary<string, int> stringCounts = new Dictionary<string, int>();

	public Dictionary<string, int> Datas
	{
		get
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (KeyValuePair<string, int> stringCount in stringCounts)
			{
				dictionary.Add(stringCount.Key, stringCount.Value);
			}
			return dictionary;
		}
	}

	public GameEventRecorderString()
	{
	}

	[JsonConstructor]
	public GameEventRecorderString(int totalCount, Dictionary<string, int> stringCounts)
		: base(totalCount)
	{
		this.stringCounts = stringCounts;
	}

	public override int Record(GameEventArgs args)
	{
		base.Record(args);
		if (args == null)
		{
			return base.totalCount;
		}
		if (!(args is GameEventArgsString { value: var value }))
		{
			return 0;
		}
		if (value.IsNullOrEmpty())
		{
			return 0;
		}
		if (stringCounts.TryAdd(value, 1))
		{
			return 1;
		}
		return ++stringCounts[value];
	}

	public int GetCount(string args)
	{
		if (!args.IsNullOrEmpty())
		{
			return stringCounts.GetValueOrDefault(args, 0);
		}
		return base.totalCount;
	}

	public int GetCount(string args, StringCompareMethod method)
	{
		int num = 0;
		foreach (KeyValuePair<string, int> stringCount in stringCounts)
		{
			if (MissionUtils.CompareString(stringCount.Key, args, method))
			{
				num += stringCount.Value;
			}
		}
		return num;
	}

	public override string ToString()
	{
		if (stringCounts.Count == 0)
		{
			return "暂无任何记录";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> stringCount in stringCounts)
		{
			stringBuilder.AppendLine($"{stringCount.Key}: {stringCount.Value}");
		}
		return stringBuilder.ToString();
	}

	public bool __ForceRemove(string id)
	{
		return stringCounts.Remove(id);
	}
}
