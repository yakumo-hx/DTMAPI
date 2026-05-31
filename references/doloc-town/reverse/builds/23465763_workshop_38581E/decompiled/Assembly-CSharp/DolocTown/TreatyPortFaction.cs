using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class TreatyPortFaction
{
	public readonly TreatyPortFactionInfo Proto;

	[JsonProperty]
	private bool isJoin;

	[JsonProperty]
	private DateInfo joinTime;

	[JsonProperty]
	private int reputationValue;

	[JsonProperty]
	private bool disableContact;

	public bool invalid => Proto == null;

	public FactionType FactionType => Proto.FactionType;

	[JsonProperty("factionName")]
	public string FactionName => Proto?.Id;

	public DateInfo JoinTime => joinTime;

	public string SettlementTime => DolocConfig.StaticTexts.TreatyPortTimeFormat.Format(joinTime.Year, joinTime.Month, joinTime.Day);

	public int CompletedMissionCount => DolocAPI.archiveHandle.cityData.factionMissionManager.GetCompletedCount(FactionType);

	public int TotalMissionCount => DolocConfig.Tables.TbFactionMission.DataList.Count((FactionMissionInfo mission) => mission.MainSeries == FactionType);

	public int ReputationValue
	{
		get
		{
			return reputationValue;
		}
		set
		{
			reputationValue = value;
		}
	}

	public bool IsJoin => isJoin;

	public bool DisableContact
	{
		get
		{
			return disableContact;
		}
		set
		{
			disableContact = value;
		}
	}

	public TreatyPortFaction(TreatyPortFactionInfo proto)
	{
		Proto = proto;
		isJoin = false;
		reputationValue = 0;
		disableContact = false;
	}

	[JsonConstructor]
	private TreatyPortFaction(string factionName, bool isJoin, DateInfo joinTime, int reputationValue, bool disableContact)
	{
		Proto = DolocConfig.Tables.TbTreatyPortFaction.GetById(factionName);
		this.isJoin = isJoin;
		this.joinTime = joinTime;
		this.reputationValue = reputationValue;
		this.disableContact = disableContact;
	}

	public void Settle(DateInfo data)
	{
		isJoin = true;
		joinTime = data;
	}

	public void OnDayChanged()
	{
		if (DisableContact && Proto.VisitingTime.HasValue && DateInfo.NextWeekDay(Proto.VisitingTime.Value) == DolocAPI.archiveHandle.timeData.dateNow.WeekDay)
		{
			disableContact = false;
		}
	}

	public void ClearSettleInfo()
	{
		isJoin = false;
		joinTime = default(DateInfo);
	}
}
