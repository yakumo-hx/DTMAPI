using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using Newtonsoft.Json;
using ParadoxNotion;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

public class GameEventRecorderManager
{
	[JsonProperty]
	private readonly Dictionary<string, GameEventRecorder> recorders = new Dictionary<string, GameEventRecorder>();

	[Command("view_game_evt_int")]
	public static void ViewGameEventRecorder(GameEventType type, int args, CompareMethod method)
	{
		Debug.Log(DolocAPI.archiveHandle.farmData.eventRecorderManager.GetCount(type, args, method));
	}

	[Command("view_game_evt_str")]
	public static void ViewGameEventRecorder(GameEventType type, string args, StringCompareMethod method)
	{
		Debug.Log(DolocAPI.archiveHandle.farmData.eventRecorderManager.GetCount(type, args, method));
	}

	private static GameEventRecorder CreateRecorder(GameEventType type)
	{
		return type.GetMissionArgsType() switch
		{
			MissionArgsType.INT => new GameEventRecorderInt(), 
			MissionArgsType.BOOL => new GameEventRecorderBool(), 
			MissionArgsType.STRING => new GameEventRecorderString(), 
			_ => new GameEventRecorder(), 
		};
	}

	private static bool ValidateType(GameEventType type, GameEventRecorder recorder)
	{
		switch (type.GetMissionArgsType())
		{
		case MissionArgsType.INT:
			return recorder is GameEventRecorderInt;
		case MissionArgsType.BOOL:
			return recorder is GameEventRecorderBool;
		case MissionArgsType.STRING:
			return recorder is GameEventRecorderString;
		case MissionArgsType.NONE:
			if (!(recorder is GameEventRecorderInt) && !(recorder is GameEventRecorderBool))
			{
				return !(recorder is GameEventRecorderString);
			}
			return false;
		default:
			return true;
		}
	}

	public GameEventRecorderManager()
	{
		foreach (GameEventType item in typeof(GameEventType).GetEnumValues().Cast<GameEventType>())
		{
			recorders.Add(item.ToString(), CreateRecorder(item));
		}
	}

	[JsonConstructor]
	public GameEventRecorderManager(Dictionary<string, GameEventRecorder> recorders)
	{
		this.recorders = recorders;
		Queue<(string, GameEventRecorder)> queue = new Queue<(string, GameEventRecorder)>();
		foreach (KeyValuePair<string, GameEventRecorder> recorder in recorders)
		{
			if (Enum.TryParse<GameEventType>(recorder.Key, ignoreCase: true, out var result) && !ValidateType(result, recorder.Value))
			{
				queue.Enqueue((recorder.Key, CreateRecorder(result)));
			}
		}
		while (queue.Count > 0)
		{
			(string, GameEventRecorder) tuple = queue.Dequeue();
			this.recorders[tuple.Item1] = tuple.Item2;
		}
		foreach (GameEventType item in typeof(GameEventType).GetEnumValues().Cast<GameEventType>())
		{
			string key = item.ToString();
			if (!this.recorders.ContainsKey(key))
			{
				this.recorders.Add(key, CreateRecorder(item));
			}
		}
	}

	public void Record(GameEventType evt, GameEventArgs args)
	{
		string key = evt.ToString();
		if (recorders.TryGetValue(key, out var value))
		{
			value.Record(args);
		}
	}

	public GameEventRecorder GetRecorder(GameEventType evt)
	{
		string key = evt.ToString();
		return recorders.GetValueOrDefault(key, null);
	}

	public int GetCount(GameEventType evt, string args)
	{
		string key = evt.ToString();
		if (!recorders.TryGetValue(key, out var value))
		{
			return 0;
		}
		if (!(value is GameEventRecorderString gameEventRecorderString))
		{
			return 0;
		}
		return gameEventRecorderString.GetCount(args);
	}

	public Dictionary<string, int> GetDatas(GameEventType evt)
	{
		string key = evt.ToString();
		if (!recorders.TryGetValue(key, out var value))
		{
			return null;
		}
		if (!(value is GameEventRecorderString gameEventRecorderString))
		{
			return null;
		}
		return gameEventRecorderString.Datas;
	}

	public int GetCount(GameEventType evt, string args, StringCompareMethod method)
	{
		string key = evt.ToString();
		if (!recorders.TryGetValue(key, out var value))
		{
			return 0;
		}
		if (!(value is GameEventRecorderString gameEventRecorderString))
		{
			return 0;
		}
		return gameEventRecorderString.GetCount(args, method);
	}

	public int GetCount(GameEventType evt, int args, CompareMethod method = CompareMethod.EqualTo)
	{
		string key = evt.ToString();
		if (!recorders.TryGetValue(key, out var value))
		{
			return 0;
		}
		if (!(value is GameEventRecorderInt gameEventRecorderInt))
		{
			return 0;
		}
		return gameEventRecorderInt.GetCount(args, method);
	}

	public int GetCount(GameEventType evt, bool args)
	{
		string key = evt.ToString();
		if (!recorders.TryGetValue(key, out var value))
		{
			return 0;
		}
		if (!(value is GameEventRecorderBool gameEventRecorderBool))
		{
			return 0;
		}
		return gameEventRecorderBool.GetCount(args);
	}

	public int GetTotalCount(GameEventType evt)
	{
		string key = evt.ToString();
		if (!recorders.TryGetValue(key, out var value))
		{
			return 0;
		}
		return value.totalCount;
	}

	public int GetAccumulation(GameEventType evt)
	{
		string key = evt.ToString();
		if (!recorders.ContainsKey(key))
		{
			return 0;
		}
		if (!(recorders[key] is GameEventRecorderInt gameEventRecorderInt))
		{
			return 0;
		}
		return gameEventRecorderInt.accumulation;
	}
}
