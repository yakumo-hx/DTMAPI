using System.Linq;

namespace DolocTown.UI;

public struct RecruitData : IUIData
{
	public bool notEmpty { get; }

	public int factionCount { get; }

	public CandidateFactionData[] factionDatas { get; }

	public RecruitData(string[] factions)
	{
		notEmpty = !factions.IsNullOrEmpty();
		factionCount = factions.Length;
		factionDatas = factions.Select((string type) => new CandidateFactionData(type)).ToArray();
	}
}
