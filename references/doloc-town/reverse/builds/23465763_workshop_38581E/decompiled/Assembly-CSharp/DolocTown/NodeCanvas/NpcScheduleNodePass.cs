using DolocTown.GameData;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Name("Pass节点", 0)]
[Description("无条件通过")]
[Icon("Sequence", false, "")]
[Color("e06b51")]
public class NpcScheduleNodePass : NpcScheduleNodeFilter
{
	public override bool IsMatch(NpcScheduleParams npcScheduleParams)
	{
		return true;
	}
}
