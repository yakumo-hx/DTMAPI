using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("开关", 0)]
[Color("ff7f50")]
public class SwitchScheduleNodeTarget : SwitchScheduleNode, ISwitchScheduleNodeTarget, ISwitchScheduleNode
{
	[SerializeField]
	private bool shouldOpen;

	public bool ShouldLight => shouldOpen;
}
