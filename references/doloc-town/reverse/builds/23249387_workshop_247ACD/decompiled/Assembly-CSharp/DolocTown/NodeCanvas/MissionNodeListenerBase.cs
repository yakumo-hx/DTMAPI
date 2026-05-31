using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

public abstract class MissionNodeListenerBase : MissionNode
{
	[SerializeField]
	[ExposeField]
	private int nodeId;

	[SerializeField]
	[ExposeField]
	private int extendId;

	[SerializeField]
	[ExposeField]
	protected bool implicitMission;

	public override int maxOutConnections => -1;

	public override MissionNodeType nodeType => MissionNodeType.MISSION;

	public override bool allowAsPrime => !IsImplicit;

	public int NodeId => nodeId;

	public int ExtendId => extendId;

	public abstract string MissionId { get; }

	public virtual bool IsLoopMission => false;

	public virtual MissionAttachModule[] AttachModules => null;

	public bool IsImplicit => implicitMission;

	public abstract MissionContent MissionContent { get; }

	public void SetNodeId(int id)
	{
		nodeId = id;
	}

	public void ClearExtendId()
	{
		extendId = 0;
	}

	public int MarkExtendId(out bool isImplicitNode)
	{
		isImplicitNode = implicitMission;
		if (!implicitMission)
		{
			return extendId++;
		}
		return extendId;
	}

	public void SetExtendId(int id)
	{
		extendId = id;
	}
}
