using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RedSaw.Web;
using UnityEngine;

namespace DolocTown.GameDataTracker;

public static class DataUploader
{
	private static string UserId
	{
		get
		{
			if (!SystemInfo.deviceUniqueIdentifier.IsNullOrEmpty())
			{
				return SystemInfo.deviceUniqueIdentifier;
			}
			return string.Empty;
		}
	}

	public static void TraceDayUpgrade(string upgradeName, int cost)
	{
		if (DolocAPI.userSettings.allowCollectData && !DolocAPI.gameManager.gameOuterConfig.enableGameConsole)
		{
			_TraceDayUpgrade(DolocAPI.archiveHandle.timeData.TotalDays, cost, upgradeName);
		}
	}

	private static void _TraceDayUpgrade(int day, int cost, string upgradeName)
	{
		JObject jObject = new JObject();
		jObject.Add("day", day);
		jObject.Add("upgrade_name", upgradeName);
		jObject.Add("cost", cost);
		jObject.Add("user_id", UserId);
		DolocAPI.DoWebRequest(WebHelper.GetPostRequest("http://1.13.18.185:8688/doloctown/tracedata/dayupgrade/upload", jObject.ToString()));
	}

	public static void TraceDayMoney(int day, int money, int totalMoney)
	{
		if (DolocAPI.userSettings.allowCollectData && !DolocAPI.gameManager.gameOuterConfig.enableGameConsole)
		{
			_TraceDayMoney(day, money, totalMoney);
		}
	}

	private static void _TraceDayMoney(int day, int money, int totalMoney)
	{
		JObject jObject = new JObject();
		jObject.Add("day", day);
		jObject.Add("money", money);
		jObject.Add("total_money", totalMoney);
		jObject.Add("user_id", UserId);
		DolocAPI.DoWebRequest(WebHelper.GetPostRequest("http://1.13.18.185:8688/doloctown/tracedata/daymoney/upload", jObject.ToString()));
	}

	public static void TraceExpressDrone(IEnumerable<(Item, int)> items)
	{
		if (DolocAPI.userSettings.allowCollectData && !DolocAPI.gameManager.gameOuterConfig.enableGameConsole)
		{
			_TraceExpressDrone(items);
		}
	}

	private static void _TraceExpressDrone(IEnumerable<(Item, int)> items)
	{
		if (items == null)
		{
			return;
		}
		JArray jArray = new JArray();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		string key;
		int value;
		foreach (var item3 in items)
		{
			Item item = item3.Item1;
			int item2 = item3.Item2;
			string text = item.name.ToLower();
			dictionary2.TryAdd(text, item2);
			if (!dictionary.TryAdd(text, 1))
			{
				key = text;
				value = dictionary[key]++;
			}
		}
		foreach (KeyValuePair<string, int> item4 in dictionary)
		{
			item4.Deconstruct(out key, out value);
			string text2 = key;
			int num = value;
			JObject jObject = new JObject();
			jObject.Add("name", text2);
			jObject.Add("count", num);
			jObject.Add("price", dictionary2[text2]);
			jArray.Add(jObject);
		}
		JObject jObject2 = new JObject();
		jObject2.Add("items", jArray);
		jObject2.Add("day", DolocAPI.archiveHandle.timeData.TotalDays);
		jObject2.Add("user_id", UserId);
		DolocAPI.DoWebRequest(WebHelper.GetPostRequest("http://1.13.18.185:8688/doloctown/tracedata/expressdrone/upload", jObject2.ToString()));
	}

	public static void TrackMissionData(string missionId, int receiveDay, int completeDay)
	{
		if (DolocAPI.userSettings.allowCollectData && !DolocAPI.gameManager.gameOuterConfig.enableGameConsole)
		{
			_TrackMissionData(missionId, receiveDay, completeDay);
		}
	}

	private static void _TrackMissionData(string missionId, int receiveDay, int completeDay)
	{
		JObject jObject = new JObject();
		jObject.Add("mission_id", missionId);
		jObject.Add("start", receiveDay);
		jObject.Add("end", completeDay);
		jObject.Add("user_id", UserId);
		DolocAPI.DoWebRequest(WebHelper.GetPostRequest("http://1.13.18.185:8688/doloctown/tracedata/daymission/upload", jObject.ToString()));
	}
}
