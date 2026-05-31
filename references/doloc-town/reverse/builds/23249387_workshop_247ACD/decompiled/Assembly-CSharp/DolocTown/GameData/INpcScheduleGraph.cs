using System.Collections.Generic;

namespace DolocTown.GameData;

public interface INpcScheduleGraph
{
	bool QuerySchedule(NpcScheduleParams status, out NpcScheduleResult result);

	IEnumerable<string> GetPortalPoints();
}
