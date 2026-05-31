using System.Collections.Generic;
using System.Linq;
using DolocTown.TreeGraph;

namespace DolocTown.GameData;

public class TechTreeDatabase
{
	private Dictionary<string, TreeGraph<TechNodeProto>> trees;

	public int totalCount => trees.Count;

	public TreeGraph<TechNodeProto>[] AllTreeGraphs => trees.Values.ToArray();

	public TechTreeDatabase()
	{
		trees = new Dictionary<string, TreeGraph<TechNodeProto>>();
	}

	public bool ContainsTree(string name)
	{
		return trees.ContainsKey(name);
	}

	public bool ContainsNode(string treeName, string nodeName)
	{
		if (trees.TryGetValue(treeName, out var value))
		{
			return value.Contains(nodeName);
		}
		return false;
	}

	public void LoadTechTrees(TreeGraph<TechNodeProto>[] trees)
	{
		foreach (TreeGraph<TechNodeProto> treeGraph in trees)
		{
			this.trees.Add(treeGraph.id, treeGraph);
		}
	}

	public bool QueryTechTree(string treeName, out TreeGraph<TechNodeProto> tree)
	{
		return trees.TryGetValue(treeName, out tree);
	}

	public bool QueryTechTreeNode(string treeName, string nodeName, out TreeGraphNode<TechNodeProto> proto)
	{
		proto = null;
		if (trees.TryGetValue(treeName, out var value))
		{
			return value.QueryNode(nodeName, out proto);
		}
		return false;
	}
}
