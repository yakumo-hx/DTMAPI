using System.Collections.Generic;
using NodeCanvas.Framework;
using NodeCanvas.Framework.Internal;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("子任务链", 0)]
[ParadoxNotion.Design.Icon("BT", false, "")]
[Color("fffde3")]
[Description("使得任务链模块化，可以在一个任务链中嵌套另一个任务链，让任务链的结构更清晰，便于维护。")]
public class MissionNodeSubGraph : MissionNode, IGraphAssignable<MissionGraph>, IGraphAssignable, IGraphElement
{
	[SerializeField]
	private MissionGraph _subGraph;

	public override int maxOutConnections => 0;

	public override Alignment2x2 iconAlignment => Alignment2x2.Bottom;

	public override MissionNodeType nodeType => MissionNodeType.SUB_GRAPH;

	Graph IGraphAssignable.subGraph
	{
		get
		{
			return _subGraph;
		}
		set
		{
			SetSubGraph(value);
		}
	}

	public MissionGraph currentInstance
	{
		get
		{
			return _subGraph;
		}
		set
		{
			SetSubGraph(value);
		}
	}

	public MissionGraph subGraph
	{
		get
		{
			return _subGraph;
		}
		set
		{
			SetSubGraph(value);
		}
	}

	Graph IGraphAssignable.currentInstance
	{
		get
		{
			return currentInstance;
		}
		set
		{
			SetSubGraph(value);
		}
	}

	public Dictionary<Graph, Graph> instances { get; set; }

	public BBParameter subGraphParameter { get; }

	public List<BBMappingParameter> variablesMap { get; set; }

	private void SetSubGraph(Graph value)
	{
		if (value == null)
		{
			_subGraph = null;
		}
		else if (value.name == base.graph.name)
		{
			Debug.LogWarning("任务链不能嵌套自己");
		}
		else
		{
			_subGraph = (MissionGraph)value;
		}
	}
}
