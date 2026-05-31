using System.Collections.Generic;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DialogueHistoryManager
{
	[JsonProperty]
	private List<DialogueBlockHistoryData> historyData { get; }

	private int replayableNodeCount => DolocAPI.GlobalParameter.ReplayableDialogueNodeCount;

	public bool notEmpty
	{
		get
		{
			if (historyData.Count > 0)
			{
				return historyData[^1].dataList.Count > 0;
			}
			return false;
		}
	}

	[JsonConstructor]
	public DialogueHistoryManager(List<DialogueBlockHistoryData> historyData = null)
	{
		this.historyData = historyData ?? new List<DialogueBlockHistoryData>();
	}

	public DialogueBlockHistoryData[] GetCurrentHistoryData()
	{
		for (int num = historyData.Count - 1; num >= 0; num--)
		{
			if (historyData[num].l10nId != DolocAPI.CurrentL10nId)
			{
				historyData.RemoveAt(num);
			}
		}
		return historyData.ToArray();
	}

	public void MarkReplayableBlock()
	{
		for (int num = historyData.Count - 1; num >= 0; num--)
		{
			if (historyData[num].dataCount == 0)
			{
				historyData.RemoveAt(num);
			}
		}
		while (historyData.Count >= replayableNodeCount)
		{
			historyData.RemoveAt(0);
		}
		historyData.Add(new DialogueBlockHistoryData(DolocAPI.CurrentL10nId, new List<DialogueLineHistoryData>()));
	}

	public void AppendLine(string npcName, string npcTitle, string content)
	{
		if (!content.IsNullOrEmpty())
		{
			InternalAppendHistory(new DialogueLineHistoryData(npcName, npcTitle, content, DialogueHistoryType.TextLine));
		}
	}

	public void AppendOption(string content)
	{
		if (!content.IsNullOrEmpty())
		{
			InternalAppendHistory(new DialogueLineHistoryData("player", DolocAPI.archiveHandle.GetPlayerName(), content, DialogueHistoryType.Option));
		}
	}

	private void InternalAppendHistory(DialogueLineHistoryData data)
	{
		if (historyData.Count != 0)
		{
			historyData[^1].dataList.Add(data);
		}
	}

	public void Clear()
	{
		historyData.Clear();
	}
}
