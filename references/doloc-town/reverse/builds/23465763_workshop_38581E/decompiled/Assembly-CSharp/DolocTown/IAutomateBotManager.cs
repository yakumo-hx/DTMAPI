using System.Collections.Generic;

namespace DolocTown;

public interface IAutomateBotManager
{
	string CurrentRoomGuid { get; }

	IEnumerable<AutomateBot> ActivatedBots { get; }

	IEnumerable<AutomateBot> AllBots { get; }

	void OnBuildingChanged(Building building, bool isRemoved);
}
