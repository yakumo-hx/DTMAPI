using System;
using System.Collections.Generic;
using System.Reflection;
using DolocTown;
using UnityEngine;

namespace RedSaw.CommandLineInterface;

public class ConsoleController<TLog> where TLog : Enum
{
	private class Timer
	{
		private readonly float interval;

		private float current;

		public Timer(float interval)
		{
			this.interval = interval;
			current = 0f;
		}

		public bool Tick(float deltaTime)
		{
			current += deltaTime;
			if (current >= interval)
			{
				current = 0f;
				return true;
			}
			return false;
		}

		public void Reset()
		{
			current = 0f;
		}
	}

	private readonly IConsoleRenderer renderer;

	private readonly IConsoleInput userInput;

	private readonly CommandSystem commandSystem;

	private readonly LogManager<TLog> logManager;

	private int alternativeCommandCount;

	private readonly bool shouldRecordFailedCommand;

	private readonly bool outputWithTime;

	private readonly bool outputStackTraceOfCommandExecution;

	private readonly bool outputCommandExceptionDetail;

	private readonly InputHistory inputHistory;

	private readonly LinearSelector selector;

	private readonly string errorColor;

	private bool ignoreTextChanged;

	private bool shouldQuitFocus;

	private readonly Timer timer = new Timer(0.12f);

	public IEnumerable<(string, string)> allLogs => logManager.allLogs;

	public string[] InputHistory
	{
		get
		{
			return inputHistory.History;
		}
		set
		{
			inputHistory.History = value;
		}
	}

	public int AlternativeOptionsCount
	{
		get
		{
			return alternativeCommandCount;
		}
		set
		{
			alternativeCommandCount = Math.Max(value, 1);
		}
	}

	public event Action OnFocusOut;

	public event Action OnFocus;

	public ConsoleController(IConsoleRenderer renderer, IConsoleInput userInput, int logCapacity = -1, int inputHistoryCapacity = 20, int commandQueryCacheCapacity = 20, int alternativeCommandCount = 8, bool shouldRecordFailedCommand = true, bool outputWithTime = true, bool outputStackTraceOfCommandExecution = true, bool outputCommandExceptionDetail = true, string errorColor = "#f27a5f")
	{
		this.renderer = renderer;
		renderer.BindOnSubmit(OnSubmit);
		renderer.BindOnTextChanged(OnTextChanged);
		this.userInput = userInput;
		commandSystem = new CommandSystem(0.3f, 0.1f, 0.1f, 20, commandQueryCacheCapacity);
		logManager = new LogManager<TLog>(logCapacity);
		logManager.OnReceivedMessage += delegate(LogManager<TLog>.Log log)
		{
			if (log.HasColor)
			{
				renderer.Output(log.message, log.color);
			}
			else
			{
				renderer.Output(log.message);
			}
		};
		inputHistory = new InputHistory(Math.Max(inputHistoryCapacity, 2));
		selector = new LinearSelector();
		selector.OnSelectionChanged += delegate(int idx)
		{
			renderer.AlternativeOptionsIndex = idx;
			renderer.MoveCursorToEnd();
		};
		this.alternativeCommandCount = Math.Max(alternativeCommandCount, 1);
		this.shouldRecordFailedCommand = shouldRecordFailedCommand;
		this.outputWithTime = outputWithTime;
		this.outputStackTraceOfCommandExecution = outputStackTraceOfCommandExecution;
		this.outputCommandExceptionDetail = outputCommandExceptionDetail;
		this.errorColor = errorColor;
	}

	public void Update(float dt)
	{
		if (renderer.IsInputFieldFocus)
		{
			if (shouldQuitFocus)
			{
				DolocAPI.UserInput.DisableAllInput();
				shouldQuitFocus = false;
			}
			if (userInput.MoveUp)
			{
				timer.Reset();
				MoveOptionUp();
			}
			else if (userInput.MoveDown)
			{
				timer.Reset();
				MoveOptionDown();
			}
			else if (userInput.MoveUpC)
			{
				if (timer.Tick(dt))
				{
					MoveOptionUp();
				}
			}
			else if (userInput.MoveDownC && timer.Tick(dt))
			{
				MoveOptionDown();
			}
		}
		else if (shouldQuitFocus)
		{
			if (userInput.Focus)
			{
				renderer.Focus();
				this.OnFocus?.Invoke();
				shouldQuitFocus = false;
			}
		}
		else
		{
			QuitFocus();
		}
	}

	public void QuitFocus()
	{
		shouldQuitFocus = true;
		this.OnFocusOut?.Invoke();
	}

	public Exception ExecuteCommand(string input, out object result)
	{
		return commandSystem.Execute(input, out result);
	}

	private void MoveOptionDown()
	{
		if (renderer.IsAlternativeOptionsActive)
		{
			selector.MoveNext();
			renderer.MoveCursorToEnd();
		}
		else
		{
			ignoreTextChanged = true;
			renderer.InputText = inputHistory.Next;
			renderer.MoveCursorToEnd();
		}
	}

	private void MoveOptionUp()
	{
		if (renderer.IsAlternativeOptionsActive)
		{
			selector.MoveLast();
			renderer.MoveCursorToEnd();
		}
		else
		{
			ignoreTextChanged = true;
			renderer.InputText = inputHistory.Last;
			renderer.MoveCursorToEnd();
		}
	}

	public Delegate GetFunction(string methodName)
	{
		return commandSystem.GetFunction(methodName);
	}

	public IEnumerable<(string, Delegate)> GetAllFunctions()
	{
		return commandSystem.GetAllFunctions();
	}

	private string AddTimeInfo(string msg)
	{
		if (outputWithTime)
		{
			return CLIUtils.TimeInfo + msg;
		}
		return msg;
	}

	private string[] AddTimeInfo(string[] msgs)
	{
		if (outputWithTime)
		{
			string timeInfo = CLIUtils.TimeInfo;
			for (int i = 0; i < msgs.Length; i++)
			{
				msgs[i] = timeInfo + msgs[i];
			}
			return msgs;
		}
		return msgs;
	}

	public void Output(string msg)
	{
		logManager.Output(AddTimeInfo(msg));
	}

	public void Output(string msg, string color)
	{
		logManager.Output(AddTimeInfo(msg), color);
	}

	public void Output(string[] msgs)
	{
		string[] array = AddTimeInfo(msgs);
		foreach (string info in array)
		{
			logManager.Output(info);
		}
	}

	public void Output(string[] msgs, string color)
	{
		string[] array = AddTimeInfo(msgs);
		foreach (string info in array)
		{
			logManager.Output(info, color);
		}
	}

	public void ClearOutputPanel()
	{
		renderer.Clear();
	}

	public void OnTextChanged(string text)
	{
		if (ignoreTextChanged)
		{
			ignoreTextChanged = false;
			return;
		}
		string inputTextToCursor = renderer.InputTextToCursor;
		Suggestion[] currentSuggestions = commandSystem.GetCurrentSuggestions(inputTextToCursor, alternativeCommandCount, CLIUtils.FindSimilarity);
		if (currentSuggestions.Length == 0)
		{
			if (renderer.IsAlternativeOptionsActive)
			{
				renderer.IsAlternativeOptionsActive = false;
			}
			return;
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		Suggestion[] array = currentSuggestions;
		for (int i = 0; i < array.Length; i++)
		{
			Suggestion suggestion = array[i];
			list.Add(suggestion.primary);
			list2.Add(suggestion.ToString());
		}
		if (!renderer.IsAlternativeOptionsActive)
		{
			renderer.IsAlternativeOptionsActive = true;
		}
		selector.LoadOptions(list);
		renderer.AlternativeOptions = list2;
		renderer.AlternativeOptionsIndex = selector.SelectionIndex;
	}

	public void OnSubmit(string text)
	{
		if (renderer.IsAlternativeOptionsActive && selector.GetCurrentSelection(out var selection))
		{
			renderer.InputTextToCursor = commandSystem.TakeSuggestion(renderer.InputTextToCursor, selection);
			renderer.IsAlternativeOptionsActive = false;
			renderer.ActivateInput();
			renderer.SetInputCursorPosition(renderer.InputText.Length);
			return;
		}
		if (renderer.IsAlternativeOptionsActive)
		{
			renderer.IsAlternativeOptionsActive = false;
		}
		Output(text);
		if (text.Length > 0)
		{
			object executeResult;
			Exception ex = commandSystem.Execute(text, out executeResult);
			if (ex == null)
			{
				inputHistory.Record(text);
				OutputResult(executeResult);
			}
			else
			{
				Debug.LogError("error occurred while execute \"" + text + "\"");
				if (!(ex is CommandExecuteException))
				{
					Debug.LogException(ex);
				}
				if (shouldRecordFailedCommand)
				{
					inputHistory.Record(text);
				}
				Output(ex.Message, errorColor);
				if (outputCommandExceptionDetail)
				{
					Output(ex.StackTrace, errorColor);
				}
			}
			renderer.InputText = string.Empty;
		}
		renderer.MoveScrollBarToEnd();
		renderer.ActivateInput();
	}

	private void OutputResult(object instance)
	{
		if (instance == null)
		{
			return;
		}
		if (instance.GetType().GetCustomAttribute<DebugObjectAttribute>() == null)
		{
			Output(instance.ToString());
			return;
		}
		(string, string)[] debugInfos = instance.GetDebugInfos();
		if (debugInfos.Length == 0)
		{
			return;
		}
		Output($"---------- {instance} start ----------");
		(string, string)[] array = debugInfos;
		for (int i = 0; i < array.Length; i++)
		{
			var (msg, text) = array[i];
			if (text.IsNullOrEmpty())
			{
				Output(msg);
			}
			else
			{
				Output(msg, text);
			}
		}
		Output($"---------- {instance} end ----------");
	}
}
