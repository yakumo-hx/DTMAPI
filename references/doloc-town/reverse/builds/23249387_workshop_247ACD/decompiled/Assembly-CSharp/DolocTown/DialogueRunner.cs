using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Yarn;
using Yarn.Markup;
using Yarn.Unity;

namespace DolocTown;

public class DialogueRunner
{
	private YarnProject project;

	private TextLineProvider lineProvider;

	private DialogueCommandRunner commandRunner;

	private Dialogue dialogue;

	private Line latestLine;

	private OptionSet latestOptionSet;

	private OptionSet.Option latestOption;

	private string defaultNpcName;

	private string currentNpcName;

	private bool checkEntrance = true;

	public bool isRunning { get; private set; }

	public bool inAsyncCommand { get; private set; }

	public bool isWaitingOptionSelection { get; private set; }

	public UnityEvent<DialogueOption[]> OnOptionsNeedPresentation { get; private set; } = new UnityEvent<DialogueOption[]>();


	public UnityEvent<string, LocalizedLine> OnLineNeedsPresentation { get; private set; } = new UnityEvent<string, LocalizedLine>();


	public UnityEvent<string> OnSetSelectedOption { get; private set; } = new UnityEvent<string>();


	public UnityEvent<string> OnNodeStart { get; private set; } = new UnityEvent<string>();


	public UnityEvent<string> OnNodeEnd { get; private set; } = new UnityEvent<string>();


	public UnityEvent OnDialogueComplete { get; private set; } = new UnityEvent();


	public UnityEvent<string> OnAsyncTaskStart { get; private set; } = new UnityEvent<string>();


	public UnityEvent<string> OnAsyncTaskEnded { get; private set; } = new UnityEvent<string>();


	public string CurrentNode
	{
		get
		{
			if (!isRunning)
			{
				return string.Empty;
			}
			return dialogue.CurrentNode;
		}
	}

	public OptionSet LatestOptionSet => latestOptionSet;

	public string DefaultNpcName
	{
		get
		{
			if (!isRunning)
			{
				return string.Empty;
			}
			return defaultNpcName;
		}
	}

	public DialogueRunner(YarnProject project, TextLineProvider lineProvider, DialogueVariableStorage variableStorage)
	{
		this.project = project;
		this.lineProvider = lineProvider;
		dialogue = new Dialogue(variableStorage)
		{
			LogDebugMessage = Debug.Log,
			LogErrorMessage = Debug.LogError,
			LineHandler = HandleLine,
			CommandHandler = HandleCommand,
			OptionsHandler = HandleOptions,
			NodeStartHandler = HandleNodeStarted,
			NodeCompleteHandler = HandleNodeEnded,
			DialogueCompleteHandler = HandleDialogueComplete,
			PrepareForLinesHandler = PrepareForLines
		};
		dialogue.SetProgram(project.Program);
		dialogue.Library.DeregisterFunction("visited");
		dialogue.Library.DeregisterFunction("visited_count");
		commandRunner = new DialogueCommandRunner(RegisterFunction);
	}

	public bool StartDialogue(string nodeName, string defaultNpcName)
	{
		if (!NodeExists(nodeName))
		{
			Debug.LogWarning("对话节点: " + nodeName + " 不存在或内容为空");
			StopDialogue();
			return false;
		}
		inAsyncCommand = false;
		isWaitingOptionSelection = false;
		if (isRunning)
		{
			Debug.LogWarning("Start a dialogue that is already running! Restart anyway.");
			StopDialogue();
		}
		this.defaultNpcName = defaultNpcName ?? string.Empty;
		currentNpcName = this.defaultNpcName;
		isRunning = true;
		dialogue.SetNode(nodeName);
		ContinueDialog();
		if (checkEntrance)
		{
			if (IsEntranceLine(latestLine))
			{
				ContinueDialog();
			}
			else if (IsEntranceOption(latestOptionSet))
			{
				dialogue.SetSelectedOption(0);
				isWaitingOptionSelection = false;
				ContinueDialog();
			}
			checkEntrance = false;
		}
		return true;
	}

	private void ContinueDialog()
	{
		if (dialogue.CurrentNode != null)
		{
			dialogue.Continue();
		}
	}

	public void StopDialogue()
	{
		dialogue.Stop();
		isRunning = false;
		isWaitingOptionSelection = false;
		inAsyncCommand = false;
	}

	private void HandleOptions(OptionSet options)
	{
		isWaitingOptionSelection = true;
		latestOptionSet = options;
		if (!checkEntrance || !IsEntranceOption(options))
		{
			DialogueOption[] array = new DialogueOption[options.Options.Length];
			for (int i = 0; i < options.Options.Length; i++)
			{
				LocalizedLine localizedLine = lineProvider.GetLocalizedLine(options.Options[i].Line);
				string line = Dialogue.ExpandSubstitutions(localizedLine.RawText, options.Options[i].Line.Substitutions);
				dialogue.LanguageCode = lineProvider.LocaleCode;
				localizedLine.Text = ParseMarkup(line);
				array[i] = new DialogueOption
				{
					TextID = options.Options[i].Line.ID,
					DialogueOptionID = options.Options[i].ID,
					Line = localizedLine,
					IsAvailable = options.Options[i].IsAvailable
				};
			}
			OnOptionsNeedPresentation.Invoke(array);
		}
	}

	public void SetSelectedOption(int optionIndex)
	{
		if (!isRunning)
		{
			Debug.LogWarning("Can't select an option when not currently running dialogue");
			return;
		}
		if (!isWaitingOptionSelection)
		{
			Debug.LogWarning("Dialogue wasn't waiting for a selection");
			return;
		}
		dialogue.SetSelectedOption(optionIndex);
		latestOption = latestOptionSet.Options[optionIndex];
		OnSetSelectedOption.Invoke(latestOption.Line.ID);
		isWaitingOptionSelection = false;
		ContinueDialog();
	}

	private void HandleCommand(Command command)
	{
		if (DolocAPI.gameManager.logDialogueCommandDebugInfo)
		{
			Debug.Log("Running dialogue command: <" + command.Text + ">");
		}
		UniTask task;
		switch (commandRunner.Execute(command.Text, out task))
		{
		case CommandExecuteResult.SucceededAsync:
			if (DolocAPI.gameManager.logDialogueCommandDebugInfo)
			{
				Debug.Log("Start async dialogue command: <" + command.Text + ">.");
			}
			StartAsyncTask(task, ContinueDialog, command).Forget();
			return;
		case CommandExecuteResult.SucceededSync:
			if (DolocAPI.gameManager.logDialogueCommandDebugInfo)
			{
				Debug.Log("Run sync dialogue command: <" + command.Text + "> Succeed.");
			}
			break;
		case CommandExecuteResult.Failed:
			Debug.LogError("Failed to run dialogue command: <" + command.Text + ">");
			break;
		}
		try
		{
			ContinueDialog();
		}
		catch (Exception)
		{
		}
	}

	private async UniTaskVoid StartAsyncTask(UniTask task, Action callback, Command command)
	{
		OnAsyncTaskStart.Invoke(dialogue.CurrentNode);
		inAsyncCommand = true;
		try
		{
			await task;
			if (DolocAPI.gameManager.logDialogueCommandDebugInfo)
			{
				Debug.Log("Run async dialogue command: <" + command.Text + "> Succeed.");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to run dialogue command: <" + command.Text + ">");
			Debug.LogError(ex?.ToString() + ex.Message);
			Debug.LogError(ex.StackTrace);
		}
		finally
		{
			inAsyncCommand = false;
			OnAsyncTaskEnded.Invoke(dialogue.CurrentNode);
			callback?.Invoke();
		}
	}

	private void HandleLine(Line line)
	{
		latestLine = line;
		if (!checkEntrance || !IsEntranceLine(line))
		{
			LocalizedLine localizedLine = lineProvider.GetLocalizedLine(line);
			string text = Dialogue.ExpandSubstitutions(localizedLine.RawText, line.Substitutions);
			text = text.Replace("|+|", "#");
			dialogue.LanguageCode = lineProvider.LocaleCode;
			localizedLine.Text = ParseMarkup(text);
			currentNpcName = localizedLine.CharacterName ?? defaultNpcName ?? string.Empty;
			if (string.IsNullOrEmpty(currentNpcName))
			{
				Debug.LogWarning("Dialogue node: <" + CurrentNode + "> does not specify an npc for line: <" + localizedLine.Text.Text + ">");
			}
			OnLineNeedsPresentation.Invoke(currentNpcName, localizedLine);
		}
	}

	private void HandleNodeStarted(string nodeName)
	{
		checkEntrance = true;
		OnNodeStart.Invoke(nodeName);
	}

	private void HandleNodeEnded(string nodeName)
	{
		OnNodeEnd.Invoke(nodeName);
	}

	private void HandleDialogueComplete()
	{
		isRunning = false;
		OnDialogueComplete.Invoke();
	}

	public void Continue()
	{
		if (inAsyncCommand || isWaitingOptionSelection || !isRunning)
		{
			return;
		}
		try
		{
			ContinueDialog();
		}
		catch (Exception)
		{
		}
	}

	public MarkupParseResult ParseMarkup(string line)
	{
		try
		{
			return dialogue.ParseMarkup(line);
		}
		catch (Exception ex)
		{
			Debug.LogError("解析MarkUp失败！<" + line + ">");
			Debug.LogError(ex.Message);
			Debug.LogError(ex.StackTrace);
			MarkupParseResult result = default(MarkupParseResult);
			result.Text = line.ClearMarkUp();
			result.Attributes = new List<MarkupAttribute>();
			return result;
		}
	}

	private void PrepareForLines(IEnumerable<string> lineIDs)
	{
		lineProvider.PrepareForLines(lineIDs);
	}

	public bool NodeExists(string nodeName)
	{
		if (!string.IsNullOrEmpty(nodeName))
		{
			return dialogue.NodeExists(nodeName);
		}
		return false;
	}

	public void RegisterFunction(string key, Delegate @delegate)
	{
		try
		{
			dialogue.Library.RegisterFunction(key, @delegate);
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
		}
	}

	private bool IsEntranceLine(Line line)
	{
		return line.ID == DialogueUtils.GetNodeLabelLineId(dialogue.CurrentNode);
	}

	private bool IsEntranceOption(OptionSet optionSet)
	{
		if (!optionSet.Options.IsNullOrEmpty())
		{
			return latestOptionSet.Options[0].Line.ID == DialogueUtils.GetNodeLabelLineId(dialogue.CurrentNode);
		}
		return false;
	}

	public bool IsNodeContainTag(string nodeName, string tag)
	{
		if (string.IsNullOrEmpty(nodeName))
		{
			return false;
		}
		return dialogue.GetTagsForNode(nodeName)?.Any((string t) => t.Equals(tag)) ?? false;
	}

	public string GetDefaultNpcNameForNode(string nodeName)
	{
		if (string.IsNullOrEmpty(nodeName) || !NodeExists(nodeName))
		{
			return string.Empty;
		}
		IEnumerable<string> tagsForNode = dialogue.GetTagsForNode(nodeName);
		if (tagsForNode == null)
		{
			return string.Empty;
		}
		foreach (string item in tagsForNode)
		{
			if (!string.IsNullOrEmpty(item) && item[0] == '@')
			{
				return item.Replace("@", "");
			}
		}
		return string.Empty;
	}
}
