using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Global;
using RedSaw;
using UnityEngine;

public class SpawnLutTest : MonoBehaviour
{
	public enum TestType
	{
		Item,
		Monster
	}

	public enum CountType
	{
		Range,
		Fixed
	}

	public TestType testType;

	public string itemLutId;

	public string monsterLutId;

	public CountType countType;

	public int spawnCount = 1;

	public Vector2Int spawnCountRange = Vector2Int.one;

	public int testCount = 1;

	public void Test()
	{
		DolocConfig.Reload();
		ISpawnLut spawnLut = testType switch
		{
			TestType.Item => DolocConfig.Tables.TbItemSpawn.GetOrDefault(itemLutId ?? ""), 
			TestType.Monster => DolocConfig.Tables.TbMonsterSpawn.GetOrDefault(monsterLutId ?? ""), 
			_ => null, 
		};
		if (spawnLut == null)
		{
			return;
		}
		int num = 0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<string, float> dictionary2 = new Dictionary<string, float>();
		Dictionary<string, SpawnData> dictionary3 = new Dictionary<string, SpawnData>();
		float num2 = spawnLut.SpawnDataList.Sum((SpawnData x) => x.SpawnWeight);
		foreach (SpawnData spawnData3 in spawnLut.SpawnDataList)
		{
			dictionary[spawnData3.SpawnId] = 0;
			dictionary2[spawnData3.SpawnId] = spawnData3.SpawnWeight / num2;
			dictionary3[spawnData3.SpawnId] = spawnData3;
		}
		int value;
		string key2;
		for (int i = 0; i < testCount; i++)
		{
			value = countType switch
			{
				CountType.Fixed => spawnCount, 
				CountType.Range => spawnCountRange.DiceCount(), 
				_ => spawnCount, 
			};
			int totalCount = value;
			foreach (KeyValuePair<SpawnData, int> item in spawnLut.SpawnInternal<SpawnData>(totalCount, null, $"第{i + 1}次生成"))
			{
				item.Deconstruct(out var key, out value);
				SpawnData spawnData = key;
				int num3 = value;
				Dictionary<string, int> dictionary4 = dictionary;
				key2 = spawnData.SpawnId;
				dictionary4[key2] += num3;
				num += num3;
			}
		}
		string text = $"==== {testCount}次测试生成总数：{num} ===\n";
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			item2.Deconstruct(out key2, out value);
			string text2 = key2;
			int num4 = value;
			SpawnData spawnData2 = dictionary3[text2];
			string text3 = (spawnData2.Unlimited ? "无限" : $"{spawnData2.MinCount} - {spawnData2.MaxCount}");
			text += $"{text2,-20} × {num4}         rate: {Math.Round((float)num4 / (float)num * 100f, 2)}%         (weight: {Math.Round(dictionary2[text2] * 100f, 2)}%)    (range: {text3})\n";
		}
		text += "========================================\n";
		Debug.Log(text);
	}
}
