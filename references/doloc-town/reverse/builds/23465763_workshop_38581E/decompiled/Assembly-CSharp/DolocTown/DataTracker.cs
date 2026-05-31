using System;
using System.IO;
using DolocTown.Config.Settings;
using DolocTown.GameData;
using DolocTown.GameDataTracker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedSaw;
using RedSaw.Web;
using UnityEngine;
using UnityEngine.Device;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DataTracker : IDataPersistence
{
	[JsonProperty]
	public readonly GameDataTrackerMissionGuide GdtMissionGuide = new GameDataTrackerMissionGuide();

	[JsonProperty]
	public readonly GameDataTrackerDayMoney GdtDayMoney = new GameDataTrackerDayMoney();

	[JsonProperty]
	public readonly ExceptionRecorder exceptionRecorderEx = new ExceptionRecorder();

	public static string PlayerLog
	{
		get
		{
			string persistentDataPath = UnityEngine.Device.Application.persistentDataPath;
			persistentDataPath = Path.Combine(persistentDataPath, "Player.log");
			if (File.Exists(persistentDataPath))
			{
				return File.ReadAllText(persistentDataPath);
			}
			return "Player.log文件未找到";
		}
	}

	public static JArray ConsoleLog
	{
		get
		{
			JArray jArray = new JArray();
			foreach (var allLog in DolocAPI.devHelper.Console.ConsoleSystem.allLogs)
			{
				JArray item = new JArray { allLog.Item1, allLog.Item2 };
				jArray.Add(item);
			}
			return jArray;
		}
	}

	public DataTracker()
	{
		UnityEngine.Device.Application.logMessageReceived += HandleException;
	}

	[JsonConstructor]
	private DataTracker(GameDataTrackerMissionGuide GdtMissionGuide = null, ExceptionRecorder exceptionRecorderEx = null, GameDataTrackerDayMoney GdtDayMoney = null)
	{
		this.GdtMissionGuide = GdtMissionGuide ?? new GameDataTrackerMissionGuide();
		this.exceptionRecorderEx = exceptionRecorderEx ?? new ExceptionRecorder();
		this.GdtDayMoney = GdtDayMoney ?? new GameDataTrackerDayMoney();
		UnityEngine.Device.Application.logMessageReceived += HandleException;
	}

	~DataTracker()
	{
		UnityEngine.Device.Application.logMessageReceived -= HandleException;
	}

	public static void ReadPlayerLog(Action<string> callback)
	{
		string persistentDataPath = UnityEngine.Device.Application.persistentDataPath;
		persistentDataPath = Path.Combine(persistentDataPath, "Player.log");
		if (!File.Exists(persistentDataPath))
		{
			callback("Player.log文件未找到");
			return;
		}
		string obj = _ReadFile(persistentDataPath);
		callback(obj);
	}

	private static string _ReadFile(string filepath)
	{
		try
		{
			using FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using StreamReader streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}
		catch (Exception ex)
		{
			return "read file error: " + ex.Message + "\n" + ex.StackTrace;
		}
	}

	private void HandleException(string message, string stackTrace, LogType logType)
	{
		if (logType == LogType.Exception && DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.OTHER_ALLOW_TRACEDATA_COLLECTOR))
		{
			_HandleException(message, stackTrace, logType);
		}
	}

	private void _HandleException(string message, string stackTrace, LogType logType)
	{
		if (!exceptionRecorderEx.Record(message, stackTrace, out var result))
		{
			DolocAPI.output("异常已被忽略");
			return;
		}
		string url = DolocTownURL.BUG_REPORT_EXCEPTION;
		_PackExceptionData(result, delegate(JObject obj)
		{
			DolocAPI.DoWebRequest(WebHelper.GetPostRequestWithDeviceInfo(url, obj));
		});
	}

	private void _PackExceptionData(ExceptionAnalyzeResult result, Action<JObject> callback)
	{
		JObject obj = result.Jsonify();
		ExceptionTriggerPoint finalTriggerPoint = result.FinalTriggerPoint;
		obj.Add("final_trigger_point", finalTriggerPoint.IsEmpty ? new JObject() : result.FinalTriggerPoint.Jsonify());
		obj.Add("console_log", ConsoleLog);
		obj.Add("version", UnityEngine.Device.Application.version);
		obj.Add("archive", DolocAPI.GetCurrentArchiveDataAsString() ?? string.Empty);
		ReadPlayerLog(delegate(string player_log)
		{
			obj.Add("player_log", player_log);
			callback(obj);
		});
	}
}
