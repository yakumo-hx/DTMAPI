using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("行为", 0)]
[Color("60b37e")]
public class NpcScheduleNodeTargetWork : NpcScheduleNode
{
	[SerializeField]
	[ExposeField]
	private NpcScheduleWorkType workType;

	[SerializeField]
	[ExposeField]
	private NpcScheduleWork workDetail;

	private bool IsWorkLoaded;

	public NpcScheduleWork Work => workDetail;

	public override string name => workType switch
	{
		NpcScheduleWorkType.Work => "工作", 
		NpcScheduleWorkType.Rest => "休息", 
		NpcScheduleWorkType.Street => "站街", 
		NpcScheduleWorkType.FellResource => "采集资源", 
		NpcScheduleWorkType.Plant => "种植", 
		NpcScheduleWorkType.Relax => "休闲", 
		_ => "未设置工作", 
	};
}
