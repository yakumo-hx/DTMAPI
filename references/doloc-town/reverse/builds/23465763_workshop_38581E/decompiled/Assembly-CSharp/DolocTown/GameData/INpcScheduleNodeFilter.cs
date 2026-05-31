using System.Collections.Generic;

namespace DolocTown.GameData;

public interface INpcScheduleNodeFilter : INpcScheduleNode
{
	IEnumerable<INpcScheduleNode> Children { get; }

	bool IsMatch(NpcScheduleParams scheduleParams);
}
