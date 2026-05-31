using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class TreatyPortFactionManager
{
	private Dictionary<string, TreatyPortFaction> _factions = new Dictionary<string, TreatyPortFaction>();

	[JsonProperty]
	private int maintainTime;

	[JsonProperty]
	private List<string> factionNames;

	private TbTreatyPortFaction Config => DolocConfig.Tables.TbTreatyPortFaction;

	[JsonProperty("totalFactions")]
	public TreatyPortFaction[] TotalFactions => _factions.Values.ToArray();

	public bool MaintainState => factionNames.Count > 0;

	public TreatyPortFactionManager()
	{
		maintainTime = 0;
		factionNames = new List<string>();
		foreach (TreatyPortFactionInfo data in Config.DataList)
		{
			_factions.TryAdd(data.Id, new TreatyPortFaction(data));
		}
	}

	[JsonConstructor]
	private TreatyPortFactionManager(TreatyPortFaction[] totalFactions, int maintainTime, string factionName, List<string> factionNames)
	{
		foreach (TreatyPortFaction treatyPortFaction in totalFactions)
		{
			if (!treatyPortFaction.invalid)
			{
				_factions.Add(treatyPortFaction.FactionName, treatyPortFaction);
			}
		}
		foreach (TreatyPortFactionInfo data in Config.DataList)
		{
			_factions.TryAdd(data.Id, new TreatyPortFaction(data));
		}
		this.maintainTime = maintainTime;
		this.factionNames = factionNames ?? new List<string>();
		if (!factionName.IsNullOrEmpty())
		{
			this.factionNames.Add(factionName);
		}
	}

	public bool QueryTreatyPortFaction(string factionName, out TreatyPortFaction faction)
	{
		faction = null;
		if (factionName.IsNullOrEmpty())
		{
			return false;
		}
		return _factions.TryGetValue(factionName, out faction);
	}

	public bool QueryTreatyPortFaction(FactionType factionType, out TreatyPortFaction faction)
	{
		return QueryTreatyPortFaction(Config.GetByFactionType(factionType).Id, out faction);
	}

	public void AddReputationValueByMissionId(string missionId, int value)
	{
		FactionMissionInfo orDefault = DolocConfig.Tables.TbFactionMission.GetOrDefault(missionId);
		AddReputationValue(Config.GetByFactionType(orDefault.MainSeries).Id, value);
	}

	public void AddReputationValue(string factionName, int value)
	{
		if (QueryTreatyPortFaction(factionName, out var faction))
		{
			faction.ReputationValue += value;
		}
	}

	public int GetReputationValue(string factionName)
	{
		if (QueryTreatyPortFaction(factionName, out var faction))
		{
			return faction.ReputationValue;
		}
		return 0;
	}

	public bool SettledTreatyPort(string factionName)
	{
		if (!QueryTreatyPortFaction(factionName, out var faction))
		{
			return false;
		}
		if (faction.ReputationValue < Config.GetById(factionName).SettledReputation)
		{
			return false;
		}
		factionNames.Add(factionName);
		maintainTime = DolocAPI.GlobalParameter.TreatyPortMaintainTime;
		string[] treatyPortGatesName = DolocAPI.GlobalParameter.TreatyPortGatesName;
		for (int i = 0; i < treatyPortGatesName.Length; i++)
		{
			DolocAPI.DisableGate(treatyPortGatesName[i]);
		}
		return true;
	}

	public void TreatyPortCompleted()
	{
		if (QueryTreatyPortFaction(Config.GetByFactionType(FactionType.Doloc).Id, out var faction))
		{
			faction.Settle(DolocAPI.archiveHandle.DateNow);
			QueryTreatyPortFaction(Config.GetByFactionType(FactionType.KonTiki).Id, out faction);
			faction.Settle(DolocAPI.archiveHandle.DateNow);
		}
	}

	public int GetJoinFactionCount()
	{
		return _factions.Values.Count((TreatyPortFaction x) => x.FactionType != FactionType.Doloc && x.IsJoin);
	}

	public int GetAllFactionCount()
	{
		return Config.DataList.Count((TreatyPortFactionInfo x) => x.FactionType != FactionType.Doloc);
	}

	public bool QueryFactionJoinState(FactionType factionType)
	{
		if (!QueryTreatyPortFaction(Config.GetByFactionType(factionType).Id, out var faction))
		{
			return false;
		}
		return faction.IsJoin;
	}

	public List<FactionType> GetJoinFactions()
	{
		List<FactionType> list = new List<FactionType>();
		foreach (TreatyPortFaction value in _factions.Values)
		{
			if (value.FactionType != FactionType.Doloc && value.IsJoin)
			{
				list.Add(value.FactionType);
			}
		}
		return list;
	}

	public string[] GetInviteFactions()
	{
		List<string> list = new List<string>();
		foreach (TreatyPortFactionInfo data in Config.DataList)
		{
			if (data.FactionType != FactionType.Doloc && QueryTreatyPortFaction(data.Id, out var faction) && !faction.IsJoin)
			{
				list.Add(faction.FactionName);
			}
		}
		return list.ToArray();
	}

	public void OnDayChanged()
	{
		foreach (TreatyPortFaction value in _factions.Values)
		{
			value.OnDayChanged();
		}
	}

	public void UpdateMaintainTime()
	{
		if (factionNames.Count == 0 || --maintainTime > 0)
		{
			return;
		}
		foreach (string factionName in factionNames)
		{
			QueryTreatyPortFaction(factionName, out var faction);
			faction.Settle(DolocAPI.archiveHandle.DateNow);
			DolocAPI.BroadcastString(GameEventType.SETTLED_TREATYPORT, factionName);
		}
		string[] treatyPortGatesName = DolocAPI.GlobalParameter.TreatyPortGatesName;
		for (int i = 0; i < treatyPortGatesName.Length; i++)
		{
			DolocAPI.EnableGate(treatyPortGatesName[i]);
		}
		factionNames.Clear();
	}
}
