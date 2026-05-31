using System;
using DolocTown.Config;
using DolocTown.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class BaseArchiveData
{
	[JsonProperty]
	public string version;

	[JsonProperty]
	public int archiveIndex;

	[JsonProperty]
	public string customPlayerName;

	[JsonProperty]
	public int money;

	[JsonConverter(typeof(StringEnumConverter))]
	public RoomType currentRoomType;

	[JsonProperty]
	public string currentScene;

	[JsonProperty]
	public DateInfo dateNow;

	[JsonProperty]
	public string realTimeStamp;

	[JsonProperty]
	public long totalGameSeconds;

	[JsonProperty]
	public ModInfo[] enabledModInfos;

	[JsonConstructor]
	public BaseArchiveData(string version, int archiveIndex, string customPlayerName, int money, RoomType currentRoomType, string currentScene, DateInfo dateNow, string realTimeStamp, long totalGameSeconds, ModInfo[] enabledModInfos)
	{
		this.version = version;
		this.archiveIndex = archiveIndex;
		this.customPlayerName = customPlayerName;
		this.money = money;
		this.currentRoomType = currentRoomType;
		this.currentScene = currentScene;
		this.dateNow = dateNow;
		this.realTimeStamp = realTimeStamp;
		this.totalGameSeconds = totalGameSeconds;
		this.enabledModInfos = enabledModInfos ?? Array.Empty<ModInfo>();
	}

	public string GetPlayerName()
	{
		if (!string.IsNullOrEmpty(customPlayerName))
		{
			return customPlayerName;
		}
		return DolocAPI.GlobalParameter.PlayerDefaultName;
	}

	public string GetPositionTitle()
	{
		return DolocConfig.Tables.TbScene.GetOrDefault(currentScene)?.Title;
	}

	public string GetGameDate()
	{
		return dateNow.GetDetailDateInfo();
	}

	public string GetMoney()
	{
		return $"{money} G";
	}

	public string GetGameTimeSpan()
	{
		int num = (int)(totalGameSeconds / 3600);
		int num2 = (int)(totalGameSeconds % 3600 / 60);
		return $"{num}:{num2:D2}";
	}

	public string GetGameRealTimeStamp()
	{
		return realTimeStamp;
	}
}
