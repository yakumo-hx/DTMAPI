using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace RedSaw.CommandLineInterface.UnityImpl;

public class GameConsole : MonoBehaviour
{
	[JsonObject]
	public struct Settings
	{
		public class Vector2Converter : JsonConverter<Vector2>
		{
			public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				float[] array = serializer.Deserialize<float[]>(reader);
				return new Vector2(array[0], array[1]);
			}

			public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
			{
				serializer.Serialize(writer, new float[2] { value.x, value.y });
			}
		}

		public class ColorConverter : JsonConverter<Color>
		{
			public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
			{
				float[] array = serializer.Deserialize<float[]>(reader);
				return new Color(array[0], array[1], array[2], array[3]);
			}

			public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
			{
				serializer.Serialize(writer, new float[4] { value.r, value.g, value.b, value.a });
			}
		}

		[JsonProperty("consolePosition")]
		[JsonConverter(typeof(Vector2Converter))]
		public Vector2 consolePosition;

		[JsonProperty("consoleSize")]
		[JsonConverter(typeof(Vector2Converter))]
		public Vector2 consoleSize;

		[JsonProperty("isActive")]
		public bool isActive;

		[JsonProperty("headerBarColor")]
		[JsonConverter(typeof(ColorConverter))]
		public Color headerBarColor;

		[JsonProperty("alternativeCommandCount")]
		public int alternativeCommandCount;

		[JsonProperty("inputHistory")]
		public string[] inputHistory;

		[JsonConstructor]
		public Settings(Vector2 consolePosition, Vector2 consoleSize, bool isActive, Color headerBarColor, int alternativeCommandCount, string[] inputHistory)
		{
			this.consolePosition = consolePosition;
			this.consoleSize = consoleSize;
			this.isActive = isActive;
			this.headerBarColor = headerBarColor;
			this.alternativeCommandCount = alternativeCommandCount;
			this.inputHistory = inputHistory;
		}
	}

	[Header("Initialize Parameters")]
	[SerializeField]
	private GameConsoleRenderer consoleRenderer;

	[SerializeField]
	private GameConsoleHeader headerBar;

	[SerializeField]
	private GameConsoleSizeGrip sizeGrip;

	[SerializeField]
	[Tooltip("the capacity of input history, at least 1")]
	private int inputHistoryCapacity = 20;

	[SerializeField]
	[Tooltip("the capacity of command query cache, at least 1")]
	private int commandQueryCacheCapacity = 20;

	[SerializeField]
	[Tooltip("alternative command options count, at least 1")]
	private int alternativeCommandCount = 8;

	[SerializeField]
	[Tooltip("should output with time information of [HH:mm:ss]")]
	private bool shouldOutputWithTime = true;

	[SerializeField]
	[Tooltip("should record failed command input")]
	private bool shouldRecordFailedCommand = true;

	[SerializeField]
	[Tooltip("should receive unity message")]
	private bool shouldReceiveUnityMessage = true;

	[SerializeField]
	[Tooltip("[debug] output virtual machine exception call stack")]
	private bool shouldOutputVMExceptionStack;

	[SerializeField]
	[Tooltip("output command exception detail")]
	private bool shouldOutputCommandExceptionDetail = true;

	[SerializeField]
	[Tooltip("initialize on awake")]
	private bool initializeOnAwake = true;

	[SerializeField]
	[Tooltip("max error continues output count")]
	private int maxErrorContinuesOutputCount = 30;

	private readonly Dictionary<Color, string> colorHexRestore = new Dictionary<Color, string>();

	private int exceptionOutputCount;

	public ConsoleController<LogType> ConsoleSystem { get; private set; }

	private string DefaultSettingsPath => Application.persistentDataPath + "/redsaw_console_settings.json";

	private void Awake()
	{
		if (initializeOnAwake)
		{
			Init();
		}
	}

	private void SaveSettings(string filepath)
	{
		AdjustPosition();
		string contents = JsonConvert.SerializeObject(new Settings(base.transform.position, ((RectTransform)consoleRenderer.transform).sizeDelta, base.gameObject.activeSelf, headerBar.HeaderBarColor, alternativeCommandCount, ConsoleSystem.InputHistory));
		File.WriteAllText(filepath, contents);
	}

	private bool LoadSettings(string filepath, out Settings settings)
	{
		if (!File.Exists(filepath))
		{
			settings = default(Settings);
			return false;
		}
		string value = File.ReadAllText(filepath);
		settings = JsonConvert.DeserializeObject<Settings>(value);
		return true;
	}

	private void ApplySettings(ConsoleController<LogType> console, Settings settings)
	{
		base.transform.position = settings.consolePosition;
		AdjustPosition();
		((RectTransform)consoleRenderer.transform).sizeDelta = settings.consoleSize;
		headerBar.HeaderBarColor = settings.headerBarColor;
		alternativeCommandCount = settings.alternativeCommandCount;
		console.AlternativeOptionsCount = settings.alternativeCommandCount;
		if (settings.inputHistory.Length != 0)
		{
			console.InputHistory = settings.inputHistory;
		}
		base.gameObject.SetActive(settings.isActive);
	}

	private void AdjustPosition()
	{
		Vector3 position = base.transform.position;
		base.transform.position = new Vector3(Mathf.Clamp(position.x, 10f, Screen.width - 100), Mathf.Clamp(position.y, 100f, Screen.height - 20), 0f);
	}

	public void Init()
	{
		if (consoleRenderer == null)
		{
			Debug.LogError("ConsoleRenderer is missing!!");
			base.gameObject.SetActive(value: false);
			return;
		}
		ConsoleSystem = new ConsoleController<LogType>(consoleRenderer, new UserInput(), -1, inputHistoryCapacity, commandQueryCacheCapacity, alternativeCommandCount, shouldRecordFailedCommand, shouldOutputWithTime, shouldOutputVMExceptionStack, shouldOutputCommandExceptionDetail);
		if (shouldReceiveUnityMessage)
		{
			Application.logMessageReceived += UnityConsoleLog;
		}
		headerBar.Init((RectTransform)base.transform);
		sizeGrip.Init(consoleRenderer.transform as RectTransform);
		if (LoadSettings(DefaultSettingsPath, out var settings))
		{
			ApplySettings(ConsoleSystem, settings);
		}
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		ConsoleSystem.Update(Time.deltaTime);
	}

	private void OnDestroy()
	{
		if (shouldReceiveUnityMessage)
		{
			Application.logMessageReceived -= UnityConsoleLog;
		}
		SaveSettings(DefaultSettingsPath);
	}

	public void QuitFocus()
	{
		ConsoleSystem.QuitFocus();
	}

	public void SaveSettings()
	{
		SaveSettings(DefaultSettingsPath);
	}

	public void Output(string msg)
	{
		ConsoleSystem.Output(msg);
	}

	public void Output(string msg, Color color)
	{
		ConsoleSystem.Output(msg, GetColor(color));
	}

	public void Output(string msg, string color)
	{
		ConsoleSystem.Output(msg, color);
	}

	private void UnityConsoleLog(string msg, string stacktrace, LogType type)
	{
		string hexColor = GetHexColor(type);
		ConsoleSystem.Output(msg, hexColor);
		if (type == LogType.Exception)
		{
			ConsoleSystem.Output(stacktrace, hexColor);
		}
	}

	private string GetHexColor(LogType type)
	{
		switch (type)
		{
		case LogType.Error:
		case LogType.Assert:
		case LogType.Exception:
			return "#b13c45";
		case LogType.Warning:
			return "#ffff00";
		default:
			return "#fffde3";
		}
	}

	public void SetReceiveUnityLog(bool value)
	{
		if (value)
		{
			Application.logMessageReceived -= UnityConsoleLog;
			Application.logMessageReceived += UnityConsoleLog;
		}
		else
		{
			Application.logMessageReceived -= UnityConsoleLog;
		}
	}

	public void AddListenerOnFocus(Action callback)
	{
		ConsoleSystem.OnFocus -= callback;
		ConsoleSystem.OnFocus += callback;
	}

	public void AddListenerOnFocusOut(Action callback)
	{
		ConsoleSystem.OnFocusOut -= callback;
		ConsoleSystem.OnFocusOut += callback;
	}

	protected string GetColor(Color color)
	{
		if (colorHexRestore.ContainsKey(color))
		{
			return colorHexRestore[color];
		}
		string text = "#" + ColorUtility.ToHtmlStringRGB(color);
		colorHexRestore.Add(color, text);
		return text;
	}

	public void ClearOutput()
	{
		ConsoleSystem.ClearOutputPanel();
	}
}
