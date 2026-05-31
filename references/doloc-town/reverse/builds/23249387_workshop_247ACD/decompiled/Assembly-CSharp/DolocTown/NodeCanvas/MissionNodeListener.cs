using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;
using UnityEngine.Serialization;

namespace DolocTown.NodeCanvas;

[Name("任务监听器", 0)]
[ParadoxNotion.Design.Icon("Eye", false, "")]
[Description("添加一个任务到目标节点并等待该任务完成后执行其下一步操作")]
public class MissionNodeListener : MissionNodeListenerBase
{
	[SerializeField]
	[ExposeField]
	private string missionId;

	[SerializeField]
	[ExposeField]
	private bool isEndEventNode;

	[SerializeField]
	[ExposeField]
	private bool isDecorator;

	[SerializeField]
	[ExposeField]
	private bool hasDecoratorId = true;

	[SerializeField]
	[ExposeField]
	private string decoratorId = string.Empty;

	[FormerlySerializedAs("missionRequire")]
	[SerializeField]
	[ExposeField]
	private MissionRequireGraphConfig missionRequireGraph;

	[SerializeField]
	[ExposeField]
	private List<MissionRequireGraphConfig> extendMissionRequires = new List<MissionRequireGraphConfig>();

	[SerializeField]
	[ExposeField]
	private MissionContentMulti.RequireMode requireMode;

	[SerializeField]
	[ExposeField]
	private List<MissionAttachModule> attachModules = new List<MissionAttachModule>();

	public override string name
	{
		get
		{
			if (isDecorator)
			{
				if (!hasDecoratorId)
				{
					return "装饰器:\"" + missionNodeName + "\"";
				}
				return "装饰器\"" + decoratorId + "\"";
			}
			return missionNodeName;
		}
	}

	private string missionNodeName => (implicitMission ? "隐式" : string.Empty) + (IsMultiListener ? "多" : string.Empty) + "任务\"" + MissionId + "\"";

	public override MissionNodeType nodeType
	{
		get
		{
			if (!isDecorator)
			{
				return MissionNodeType.MISSION;
			}
			return MissionNodeType.DECORATOR;
		}
	}

	public IEnumerable<MissionNodeListener> Decorators
	{
		get
		{
			foreach (Node item in from c in base.outConnections
				where ((MissionConnection)c).IsAvailable
				select c.targetNode)
			{
				if (item is MissionNodeListener { isDecorator: not false } missionNodeListener)
				{
					yield return missionNodeListener;
				}
			}
		}
	}

	private Color CurrentNodeColor => ColorUtils.HexToColor(isEndEventNode ? "f72b64" : (isDecorator ? "ffcd75" : (implicitMission ? "92e8c0" : "fffde3")));

	public bool IsMultiListener => extendMissionRequires.Count > 0;

	public override string MissionId => base.graph.name + missionId;

	public bool IsDecorator => isDecorator;

	public bool IsEndEventNode => isEndEventNode;

	public string DecoratorId
	{
		get
		{
			if (!hasDecoratorId)
			{
				return null;
			}
			return decoratorId;
		}
	}

	public override MissionAttachModule[] AttachModules => attachModules.ToArray();

	public override MissionContent MissionContent
	{
		get
		{
			if (!IsMultiListener)
			{
				return SingleMissionContent;
			}
			return MultiMissionContent;
		}
	}

	private MissionContent SingleMissionContent => new MissionContentSingle(MainMissionRequire);

	private MissionContent MultiMissionContent
	{
		get
		{
			int currentId = 1;
			List<MissionRequire> list = new List<MissionRequire>();
			list.Add(MainMissionRequire);
			list.AddRange(extendMissionRequires.Select((MissionRequireGraphConfig MR) => GetMissionRequire(currentId++, MR)));
			return new MissionContentMulti(list.ToArray(), requireMode);
		}
	}

	public MissionRequire[] MissionRequires
	{
		get
		{
			List<MissionRequire> tRequires = new List<MissionRequire> { MainMissionRequire };
			tRequires.AddRange(extendMissionRequires.Select((MissionRequireGraphConfig MR) => GetMissionRequire(tRequires.Count, MR)));
			return tRequires.ToArray();
		}
	}

	private MissionRequire MainMissionRequire => GetMissionRequire(0, missionRequireGraph);

	public bool IsAnyRequireGraphMatch<T>(bool isGlobalCount, bool hasCondition) where T : MissionRequireTemplate
	{
		if (missionRequireGraph.IsMatch<T>(isGlobalCount, hasCondition))
		{
			return true;
		}
		if (extendMissionRequires.IsNullOrEmpty())
		{
			return false;
		}
		return extendMissionRequires.Any((MissionRequireGraphConfig MR) => MR.IsMatch<T>(isGlobalCount, hasCondition));
	}

	private MissionRequire GetMissionRequire(int id, MissionRequireGraphConfig graphConfig)
	{
		DialogueConditionTask condition = (graphConfig.hasAdditionalCondition ? graphConfig.additionalCondition : null);
		return new MissionRequire_Graph(base.graph.name, MissionId, base.UID, id, graphConfig.RequireTemplate, condition, graphConfig.customMissionProgressFunc);
	}

	public bool TryGetParentMissionNode(out MissionNodeListener parentNode)
	{
		foreach (Connection inConnection in base.inConnections)
		{
			if (inConnection.sourceNode is MissionNodeListener missionNodeListener)
			{
				parentNode = missionNodeListener;
				return true;
			}
		}
		parentNode = null;
		return false;
	}

	public bool QueryMissionRewards(out List<Reward> rewards, out bool shouldSendAsEmail)
	{
		foreach (Connection item in base.outConnections.Where((Connection c) => ((MissionConnection)c).IsAvailable))
		{
			if (item.targetNode is MissionNodeReward missionNodeReward)
			{
				rewards = CollectRewardsOfDecorators(new List<Reward>(missionNodeReward.MissionRewards));
				shouldSendAsEmail = missionNodeReward.ShouldSendEmail;
				return true;
			}
		}
		shouldSendAsEmail = false;
		rewards = null;
		return false;
	}

	public bool QueryRequire(int requireId, out MissionRequireTemplate template, out DialogueConditionTask condition, out CustomMissionProgressFunc customMissionProgressFunc)
	{
		condition = null;
		template = null;
		customMissionProgressFunc = null;
		if (requireId < 0 || requireId - 1 >= extendMissionRequires.Count)
		{
			return false;
		}
		MissionRequireGraphConfig missionRequireGraphConfig = ((requireId == 0) ? missionRequireGraph : extendMissionRequires[requireId - 1]);
		condition = (missionRequireGraphConfig.hasAdditionalCondition ? missionRequireGraphConfig.additionalCondition : null);
		template = missionRequireGraphConfig.RequireTemplate;
		customMissionProgressFunc = missionRequireGraphConfig.customMissionProgressFunc;
		return template != null;
	}

	private List<Reward> CollectRewardsOfDecorators(List<Reward> current)
	{
		foreach (MissionNodeListener decorator in Decorators)
		{
			if (!decorator.QueryMissionRewards(out var rewards, out var _))
			{
				continue;
			}
			if (!DolocAPI.IsEventDecoratorComplete(decorator.decoratorId))
			{
				Debug.Log("装饰器" + decorator.decoratorId + "未完成，无法获取奖励");
				continue;
			}
			foreach (Reward item in rewards)
			{
				Reward.CombineRewards(current, item);
			}
		}
		return current;
	}

	public MissionConnection GetConnectionToNode(MissionNodeListener listener)
	{
		foreach (Connection outConnection in base.outConnections)
		{
			if (outConnection.targetNode == listener)
			{
				return (MissionConnection)outConnection;
			}
		}
		return null;
	}
}
