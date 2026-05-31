using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Dialogue;
using DolocTown.Config.NPC;
using DolocTown.Config.Time;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Yarn;
using Yarn.Markup;
using Yarn.Unity;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DialogueManager
{
	[JsonProperty]
	private DialogueVariableStorage variableStorage;

	[JsonProperty]
	private Dictionary<string, DialogueData> dialogueDatas;

	[JsonProperty]
	private string defaultNpcName;

	[JsonProperty]
	private Queue<DialogueNodeData> unhandledDialogueNodes;

	[JsonProperty]
	private Dictionary<string, int> visitedArgs;

	[JsonProperty]
	private Dictionary<string, DateInfo> timeStamps;

	private DialogueRunner dialogueRunner;

	private static YarnProject project;

	private static TextLineProvider lineProvider;

	[JsonProperty]
	public DialogueHistoryManager historyManager { get; }

	public string CurrentNode => dialogueRunner.CurrentNode;

	public OptionSet LatestOptionSet => dialogueRunner.LatestOptionSet;

	public string DefaultNpcName => dialogueRunner.DefaultNpcName;

	public Queue<DialogueNodeData> UnhandledDialogueNodes => unhandledDialogueNodes;

	public bool IsDialogueRunning => dialogueRunner.isRunning;

	public bool IsWaitingSelection => dialogueRunner.isWaitingOptionSelection;

	public UnityEvent<DialogueOption[]> OnOptionsNeedPresentation => dialogueRunner.OnOptionsNeedPresentation;

	public UnityEvent<string, LocalizedLine> OnLineNeedsPresentation => dialogueRunner.OnLineNeedsPresentation;

	public UnityEvent<string> OnSetSelectedOption => dialogueRunner.OnSetSelectedOption;

	public UnityEvent<string> OnNodeStart => dialogueRunner.OnNodeStart;

	public UnityEvent<string> OnNodeEnd => dialogueRunner.OnNodeEnd;

	public UnityEvent OnDialogueComplete => dialogueRunner.OnDialogueComplete;

	public UnityEvent<string> OnAsyncTaskStarted => dialogueRunner.OnAsyncTaskStart;

	[JsonConstructor]
	public DialogueManager(string defaultNpcName = null, DialogueVariableStorage variableStorage = null, Dictionary<string, DialogueData> dialogueDatas = null, Queue<DialogueNodeData> unhandledDialogueNodes = null, Dictionary<string, int> visitedArgs = null, Dictionary<string, DateInfo> timeStamps = null, DialogueHistoryManager historyManager = null)
	{
		if ((object)project == null)
		{
			project = DolocAPI.gameManager.gameInitConfig.yarnProject;
		}
		if (lineProvider == null)
		{
			lineProvider = new TextLineProvider(project);
		}
		this.defaultNpcName = defaultNpcName ?? string.Empty;
		this.variableStorage = variableStorage ?? new DialogueVariableStorage();
		this.dialogueDatas = dialogueDatas ?? new Dictionary<string, DialogueData>();
		this.unhandledDialogueNodes = unhandledDialogueNodes ?? new Queue<DialogueNodeData>();
		this.visitedArgs = visitedArgs ?? new Dictionary<string, int>();
		this.timeStamps = timeStamps ?? new Dictionary<string, DateInfo>();
		this.historyManager = historyManager ?? new DialogueHistoryManager();
		SwitchLanguage(DolocAPI.CurrentL10nId);
		dialogueRunner = new DialogueRunner(project, lineProvider, this.variableStorage);
	}

	public void SwitchLanguage(string l10nId)
	{
		lineProvider.SwitchLanguage(l10nId);
	}

	public bool CheckEventStatus(string npcName)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value))
		{
			if (IsEventNode(value.GetEntrance()))
			{
				return true;
			}
			if (!value.overrideEntrance.IsNullOrEmpty())
			{
				return false;
			}
			string[] allCandidateNodes = value.allCandidateNodes;
			foreach (string nodeName in allCandidateNodes)
			{
				if (IsEventNode(nodeName))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CheckDialogueStatus(string npcName)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value))
		{
			return value.HasDialogueNode;
		}
		return false;
	}

	public bool AddDialogueNode(string nodeName, string npcName)
	{
		if (!NodeExists(nodeName))
		{
			return false;
		}
		dialogueDatas.TryAdd(npcName, new DialogueData(npcName));
		return dialogueDatas[npcName].AddDialogueNode(nodeName);
	}

	public bool ContainsNode(string nodeName, string npcName)
	{
		if (!dialogueDatas.TryGetValue(npcName, out var value))
		{
			return false;
		}
		return value.Contains(nodeName);
	}

	public bool CheckDialogueData(string npcName)
	{
		return dialogueDatas.ContainsKey(npcName);
	}

	public bool GetEntrance(string npcName, out string entranceNode)
	{
		entranceNode = (dialogueDatas.TryGetValue(npcName, out var value) ? value.GetEntrance() : null);
		return !string.IsNullOrEmpty(entranceNode);
	}

	public bool SetEntrance(string nodeName, string npcName)
	{
		if (!NodeExists(nodeName))
		{
			return false;
		}
		dialogueDatas.TryAdd(npcName, new DialogueData(npcName));
		dialogueDatas[npcName].SetEntrance(nodeName);
		return true;
	}

	public void RemoveEntrance(string npcName)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value))
		{
			value.SetEntrance(null);
		}
	}

	public bool SetOverrideEntrance(string nodeName, string npcName)
	{
		if (!NodeExists(nodeName))
		{
			return false;
		}
		dialogueDatas.TryAdd(npcName, new DialogueData(npcName));
		dialogueDatas[npcName].SetOverrideEntrance(nodeName);
		return true;
	}

	public void RemoveOverrideEntrance(string npcName)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value))
		{
			value.SetOverrideEntrance(null);
		}
	}

	public string[] GetLocalizedCandidateLines(string npcName)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value))
		{
			if (!value.useCandidateNodes)
			{
				return null;
			}
			List<string> list = new List<string>();
			string[] allCandidateNodes = value.allCandidateNodes;
			foreach (string nodeName in allCandidateNodes)
			{
				string localizedText = GetLocalizedText(DialogueUtils.GetNodeLabelLineId(nodeName));
				if (!string.IsNullOrEmpty(localizedText))
				{
					list.Add(localizedText);
				}
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return list.ToArray();
		}
		return null;
	}

	public bool GetCandidateNodes(string npcName, out string[] nodeNames)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value))
		{
			nodeNames = value.allCandidateNodes;
			return true;
		}
		nodeNames = Array.Empty<string>();
		return false;
	}

	private bool CheckIdleNodeCondition(IdleTalkNodeInfo node, string npcName)
	{
		if (NodeExists(node.Id) && (node.Weather.IsNullOrEmpty() || node.Weather.Contains(DolocAPI.archiveHandle.CurrentWeatherType)) && (node.Month.IsNullOrEmpty() || node.Month.Contains(DolocAPI.archiveHandle.DateNow.Month)) && (node.HourRange.IsNullOrEmpty() || node.HourRange.Any((TimeRange x) => x.InRange(DolocAPI.archiveHandle.DateNow.Hour, ignoreEqual: false))) && (node.LikabilityRange.IsNullOrEmpty() || node.LikabilityRange.Any((TimeRange x) => x.InRange(DolocAPI.QueryNpcLikingLv(npcName), ignoreEqual: false))) && (node.EventDecorator.IsNullOrEmpty() || node.EventDecorator.Any(DolocAPI.IsEventDecoratorComplete)) && (node.MarkPoint.IsNullOrEmpty() || node.MarkPoint.Any((string x) => DolocAPI.IsNpcAtMarkPoint(npcName, x))))
		{
			if (!node.RoomId.IsNullOrEmpty())
			{
				return node.RoomId.Any((string x) => DolocAPI.CurrentRoom.RoomId == x || DolocAPI.CurrentRoom.SceneRawName == x);
			}
			return true;
		}
		return false;
	}

	public bool GetRandomIdleTalkNode(string npcName, out string selectedNode, out List<string> validNodes)
	{
		validNodes = new List<string>();
		selectedNode = string.Empty;
		List<IdleTalkNodeInfo> npcNodeByOrder = DolocConfig.Tables.TbIdleTalkNode.GetNpcNodeByOrder(npcName);
		if (npcNodeByOrder.IsNullOrEmpty())
		{
			return false;
		}
		int? num = null;
		foreach (IdleTalkNodeInfo item in npcNodeByOrder)
		{
			if (CheckIdleNodeCondition(item, npcName))
			{
				if (!num.HasValue)
				{
					num = item.Order;
				}
				else if (item.Order < num)
				{
					break;
				}
				validNodes.Add(item.Id);
			}
		}
		if (validNodes.Count == 0)
		{
			return false;
		}
		selectedNode = validNodes[UnityEngine.Random.Range(0, validNodes.Count)];
		return true;
	}

	public bool GetCandidateNodeByIndex(string npcName, int index, out string nodeName)
	{
		if (dialogueDatas.TryGetValue(npcName, out var value) && index >= 0 && index < value.candidateCount)
		{
			nodeName = value.allCandidateNodes[index];
			return true;
		}
		nodeName = null;
		return false;
	}

	public bool RemoveDialogueNode(string nodeName, string npcName)
	{
		if (dialogueDatas.ContainsKey(npcName))
		{
			return dialogueDatas[npcName].RemoveDialogueNode(nodeName);
		}
		return false;
	}

	public string GetLocalizedText(string lineId)
	{
		return project.GetLocalization(lineProvider.LocaleCode)?.GetLocalizedString(lineId) ?? ("missing:<" + lineId + ">");
	}

	public bool NodeExists(string nodeName)
	{
		if (dialogueRunner.NodeExists(nodeName))
		{
			return true;
		}
		Debug.LogError("Dialogue node " + nodeName + " not exists!");
		return false;
	}

	public string GetDefaultNpcNameForNode(string nodeName)
	{
		return dialogueRunner.GetDefaultNpcNameForNode(nodeName);
	}

	public bool IsEventNode(string nodeName)
	{
		return dialogueRunner.IsNodeContainTag(nodeName, "is_event");
	}

	public bool IsResidentNode(string nodeName)
	{
		return dialogueRunner.IsNodeContainTag(nodeName, "is_resident");
	}

	public bool IsCancelable(string nodeName)
	{
		if (IsResidentNode(nodeName))
		{
			return !dialogueRunner.IsNodeContainTag(nodeName, "not_cancelable");
		}
		return dialogueRunner.IsNodeContainTag(nodeName, "is_cancelable");
	}

	public bool DisableTrackingOption(string nodeName)
	{
		return dialogueRunner.IsNodeContainTag(nodeName, "disable_tracking_option");
	}

	public bool UseTalkAnim(string nodeName)
	{
		return !dialogueRunner.IsNodeContainTag(nodeName, "disable_talk_anim");
	}

	public bool UseCinemaScreen(string nodeName)
	{
		return !dialogueRunner.IsNodeContainTag(nodeName, "disable_cinema_screen");
	}

	public bool StartDialogue(string nodeName, string defaultNpcName)
	{
		this.defaultNpcName = defaultNpcName ?? this.defaultNpcName;
		return dialogueRunner.StartDialogue(nodeName, this.defaultNpcName);
	}

	public bool RunCommands(string nodeName)
	{
		return dialogueRunner.StartDialogue(nodeName, "");
	}

	public void Continue()
	{
		dialogueRunner.Continue();
	}

	public void SetSelectedOption(int optionIndex)
	{
		dialogueRunner.SetSelectedOption(optionIndex);
	}

	public void StopDialogue()
	{
		dialogueRunner.StopDialogue();
	}

	public MarkupParseResult ParseMarkup(string text)
	{
		return dialogueRunner.ParseMarkup(text);
	}

	public string GetTextWithMarkup(MarkupParseResult markupLine)
	{
		string text = markupLine.Text;
		if (markupLine.Attributes.Count == 0)
		{
			return markupLine.Text;
		}
		foreach (MarkupAttribute item in markupLine.Attributes.OrderByDescending((MarkupAttribute attr) => attr.Position + attr.Length).ToList())
		{
			DialogueTextStyleInfo orDefault = DolocConfig.Tables.TbDialogueTextStyle.GetOrDefault(item.Name);
			if (orDefault == null)
			{
				continue;
			}
			orDefault.Labels_Index.TryGetValue(DolocAPI.userSettings.useTextShakeEffect, out var value2);
			if (value2 != null)
			{
				if (!value2.CloseLabel.IsNullOrEmpty())
				{
					text = text.Insert(item.Position + item.Length, value2.CloseLabel);
				}
				string value3 = item.Properties.Values.Aggregate(value2.OpenLabel, (string current, MarkupValue value) => current.Format(value));
				text = text.Insert(item.Position, value3);
			}
		}
		return text;
	}

	public void Visit(string id)
	{
		visitedArgs.TryAdd(id, 0);
		visitedArgs[id]++;
	}

	public bool HasVisited(string id)
	{
		return visitedArgs.ContainsKey(id);
	}

	public int GetVisitedCount(string id)
	{
		if (!visitedArgs.TryGetValue(id, out var value))
		{
			return 0;
		}
		return value;
	}

	public void ClearVisitedCount(string id)
	{
		visitedArgs.Remove(id);
	}

	public void MarkTimeStamp(string id, DateInfo dateInfo)
	{
		timeStamps[id] = dateInfo;
	}

	public bool GetTimeStamp(string id, out DateInfo dateInfo)
	{
		return timeStamps.TryGetValue(id, out dateInfo);
	}

	public void ClearTimeStamp(string id)
	{
		timeStamps.Remove(id);
	}
}
