using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Archives;
using DolocTown.Config.Settings;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class ChipDocumentManager
{
	[JsonProperty]
	public List<string> unlockedOrder = new List<string>();

	public Dictionary<string, ChipDocumentInfo> unlockedDocuments = new Dictionary<string, ChipDocumentInfo>();

	[JsonProperty]
	private int totalChipCount;

	[JsonProperty]
	private DateInfo submitTimeStamp;

	private int latestUnlockedDocCount;

	private SortedDictionary<int, List<string>> randomChipDocumentPool = new SortedDictionary<int, List<string>>();

	[JsonProperty]
	private List<int> unanalyzedChips = new List<int>();

	private GlobalParameterInfo config => DolocAPI.GlobalParameter;

	private int maxChipCount => ChipDocTb.DataList.Count;

	public bool allChipDocUnlocked => totalChipCount == maxChipCount;

	private TbSpecialChipEvent ChipEventTb => DolocConfig.Tables.TbSpecialChipEvent;

	private TbChipDocument ChipDocTb => DolocConfig.Tables.TbChipDocument;

	public int chipSubmitLimit => config.ChipSubmitLimit;

	public string chipItemName => config.ChipItemName;

	public int SubmittedChipCount => totalChipCount;

	public ChipDocumentManager()
	{
		foreach (ChipDocumentInfo data in ChipDocTb.DataList)
		{
			if (data.PoolId >= 0)
			{
				randomChipDocumentPool.TryAdd(data.PoolId, new List<string>());
				randomChipDocumentPool[data.PoolId].Add(data.Id);
			}
		}
	}

	[JsonConstructor]
	private ChipDocumentManager(List<string> unlockedOrder, List<int> unanalyzedChips, int totalChipCount, DateInfo submitTimeStamp)
	{
		this.unlockedOrder = unlockedOrder;
		this.unanalyzedChips = unanalyzedChips;
		this.totalChipCount = totalChipCount;
		this.submitTimeStamp = submitTimeStamp;
		foreach (string item in unlockedOrder)
		{
			unlockedDocuments.Add(item, ChipDocTb.Get(item));
		}
		foreach (ChipDocumentInfo data in ChipDocTb.DataList)
		{
			if (data.PoolId >= 0 && !unlockedOrder.Contains(data.Id))
			{
				randomChipDocumentPool.TryAdd(data.PoolId, new List<string>());
				randomChipDocumentPool[data.PoolId].Add(data.Id);
			}
		}
	}

	private int SubmitChip(int chipCount)
	{
		if (allChipDocUnlocked)
		{
			return 0;
		}
		int num = totalChipCount;
		totalChipCount = Mathf.Min(totalChipCount + chipCount, maxChipCount);
		for (int i = num + 1; i <= totalChipCount; i++)
		{
			unanalyzedChips.Add(i);
			Debug.Log(ChipEventTb.DataMap.ContainsKey(i) ? $"特殊芯片提交数：{i}" : $"普通芯片提交：{i}");
		}
		submitTimeStamp = DolocAPI.archiveHandle.DateNow;
		latestUnlockedDocCount = 0;
		return totalChipCount - num;
	}

	public int ChipRemainingAnalysisHour()
	{
		int num = Mathf.CeilToInt((float)(unanalyzedChips.Count * config.ChipAnalyzeHour) * Mathf.Pow(config.ChipAnalyzeSpeedBuff, unanalyzedChips.Count - 1));
		int num2 = submitTimeStamp.CalHourDiff(DolocAPI.archiveHandle.DateNow);
		return Mathf.Max(0, num - num2);
	}

	public string GetLatestChipDocTitle()
	{
		int num = unlockedOrder.Count - 1;
		string text = string.Empty;
		for (int i = 0; i < latestUnlockedDocCount; i++)
		{
			if (num < 0)
			{
				break;
			}
			string key = unlockedOrder[num--];
			if (ChipDocTb.DataMap.TryGetValue(key, out var value))
			{
				text += value.Title;
			}
		}
		return text;
	}

	public int GetLatestChipDocCount()
	{
		return latestUnlockedDocCount;
	}

	public void AnalyzeChips()
	{
		latestUnlockedDocCount = 0;
		bool flag = false;
		foreach (int item in unanalyzedChips.ToList())
		{
			if (ChipEventTb.DataMap.ContainsKey(item))
			{
				SpecialChipEventInfo specialChipEventInfo = ChipEventTb.Get(item);
				Debug.Log($"分析特殊芯片：第{item}枚");
				foreach (ChipEvent chipEvent in specialChipEventInfo.ChipEvents)
				{
					HandleChipEvent(chipEvent.EventType, chipEvent.TargetId);
				}
				flag = true;
			}
			else
			{
				UnlockChipDocumentRandom();
			}
		}
		unanalyzedChips.Clear();
		if (!flag)
		{
			DolocAPI.StartDialogueNode(config.ChipProgressCheckDialogueNode);
		}
		DolocAPI.BroadcastInt(GameEventType.ANALYZE_CHIP, 1);
	}

	private void HandleChipEvent(ChipEventType type, string id)
	{
		switch (type)
		{
		case ChipEventType.DialogueTree:
			DolocAPI.StartDialogueNode(id);
			break;
		case ChipEventType.Document:
			UnlockChipDocumentById(id);
			break;
		case ChipEventType.Equipment:
			DolocAPI.archiveHandle.UnlockRecipe(id);
			break;
		default:
			Debug.LogError($"未处理的特殊芯片事件类型: {type}");
			break;
		}
	}

	private bool UnlockChipDocumentRandom()
	{
		for (int i = 0; i < randomChipDocumentPool.Count; i++)
		{
			List<string> list = randomChipDocumentPool.Values.ToArray()[0];
			if (list.IsNullOrEmpty())
			{
				randomChipDocumentPool.Remove(i);
				continue;
			}
			int index = new System.Random().Next(list.Count);
			string text = list[index];
			list.Remove(text);
			if (!ChipDocTb.DataMap.TryGetValue(text, out var value))
			{
				Debug.LogError("没有配置数据: " + text);
				return false;
			}
			unlockedDocuments.Add(text, value);
			unlockedOrder.Add(text);
			latestUnlockedDocCount++;
			Debug.Log("解锁随机档案: " + text);
			return true;
		}
		Debug.LogWarning("随机芯片档案已全部解锁");
		return false;
	}

	public bool UnlockChipDocumentById(string chipDocId)
	{
		if (!ChipDocTb.DataMap.TryGetValue(chipDocId, out var value))
		{
			Debug.LogError("没有配置数据: " + chipDocId);
			return false;
		}
		if (!unlockedDocuments.TryAdd(chipDocId, value))
		{
			Debug.LogWarning("档案已解锁：" + chipDocId);
			return false;
		}
		unlockedOrder.Add(chipDocId);
		latestUnlockedDocCount++;
		Debug.Log("解锁特殊档案: " + chipDocId);
		return true;
	}

	public bool CanSubmitItemAsChip(Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (item.name == chipItemName)
		{
			return totalChipCount < ChipDocTb.DataList.Count;
		}
		return false;
	}

	public int SubmitItemCountAsChip(Item item)
	{
		if (item.name == chipItemName)
		{
			return SubmitChip(Mathf.Min(chipSubmitLimit, item.count));
		}
		return 0;
	}

	public void SubmitFirstChip()
	{
		if (totalChipCount == 0)
		{
			SubmitChip(1);
		}
	}

	public bool HasSpecialChipToAnalyze()
	{
		foreach (int unanalyzedChip in unanalyzedChips)
		{
			if (ChipEventTb.DataMap.ContainsKey(unanalyzedChip))
			{
				return true;
			}
		}
		return false;
	}

	public bool HasNormalChipToAnalyze()
	{
		foreach (int unanalyzedChip in unanalyzedChips)
		{
			if (!ChipEventTb.DataMap.ContainsKey(unanalyzedChip))
			{
				return true;
			}
		}
		return false;
	}
}
