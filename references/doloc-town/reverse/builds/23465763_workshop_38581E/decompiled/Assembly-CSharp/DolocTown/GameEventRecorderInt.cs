using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ParadoxNotion;
using UnityEngine;

namespace DolocTown;

public class GameEventRecorderInt : GameEventRecorder
{
	[JsonProperty]
	public int accumulation { get; private set; }

	[JsonProperty]
	public Dictionary<int, int> intCounts { get; private set; } = new Dictionary<int, int>();


	public GameEventRecorderInt()
	{
	}

	[JsonConstructor]
	public GameEventRecorderInt(int totalCount, int accumulation, Dictionary<int, int> intCounts = null)
		: base(totalCount)
	{
		this.accumulation = accumulation;
		this.intCounts = intCounts ?? new Dictionary<int, int>();
	}

	public override int Record(GameEventArgs args)
	{
		if (!(args is GameEventArgsInt gameEventArgsInt))
		{
			return 0;
		}
		base.Record(args);
		accumulation += gameEventArgsInt.value;
		if (!intCounts.TryAdd(gameEventArgsInt.value, 1))
		{
			intCounts[gameEventArgsInt.value]++;
		}
		return intCounts[gameEventArgsInt.value];
	}

	public void UnRecordAccumulation(int value)
	{
		accumulation -= Mathf.Min(value, accumulation);
	}

	public void UnRecord(int value)
	{
		if (intCounts.ContainsKey(value))
		{
			accumulation -= value;
			intCounts[value]--;
			if (intCounts[value] == 0)
			{
				intCounts.Remove(value);
			}
		}
	}

	public int GetCount(int value, CompareMethod method)
	{
		int num = 0;
		foreach (KeyValuePair<int, int> intCount in intCounts)
		{
			if (OperationTools.Compare(intCount.Key, value, method))
			{
				num += intCount.Value;
			}
		}
		return num;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, int> intCount in intCounts)
		{
			stringBuilder.AppendLine($"{intCount.Key}:{intCount.Value}");
		}
		stringBuilder.AppendLine("累加总数: " + accumulation);
		return stringBuilder.ToString();
	}
}
