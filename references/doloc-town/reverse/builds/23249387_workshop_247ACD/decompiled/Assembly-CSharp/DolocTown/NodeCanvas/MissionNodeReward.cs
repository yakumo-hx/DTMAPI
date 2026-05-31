using System.Collections.Generic;
using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("奖励节点", 0)]
[ParadoxNotion.Design.Icon("UpwardsArrow", false, "")]
[Color("fffde3")]
[Description("该任务链的奖励，待完成后发放奖励")]
public class MissionNodeReward : MissionNode
{
	[SerializeField]
	[ExposeField]
	private bool shouldSendEmail;

	[SerializeField]
	[ExposeField]
	private List<MissionRewardCfg> _missionRewardCfgs = new List<MissionRewardCfg>();

	public override string name => "任务链奖励节点";

	public override int maxOutConnections => 0;

	public override MissionNodeType nodeType => MissionNodeType.REWARD;

	public bool ShouldSendEmail => shouldSendEmail;

	public IEnumerable<Reward> MissionRewards
	{
		get
		{
			if (_missionRewardCfgs.Count == 0)
			{
				yield return null;
			}
			foreach (MissionRewardCfg missionRewardCfg in _missionRewardCfgs)
			{
				yield return DolocAPI.CreateReward(new RewardProto(missionRewardCfg.RewardType, missionRewardCfg.content, missionRewardCfg.count));
			}
		}
	}
}
