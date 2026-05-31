using System.Collections.Generic;

namespace DolocTown.GameData;

public interface ISwitchScheduleNodeFilter : ISwitchScheduleNode
{
	IEnumerable<ISwitchScheduleNode> Children { get; }

	bool IsMatch(SwitchScheduleParams param);
}
