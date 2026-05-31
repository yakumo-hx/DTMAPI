using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.TreeGraph;

public class TreeGraph<T>
{
	public readonly string id;

	public readonly TreeGraphNode<T>[] nodes;

	public readonly TreeGraphLink[] links;

	public readonly Vector2Int defaultPos;

	public readonly TreeGraphNode<T> defaultNode;

	public readonly Vector2Int size;

	private Dictionary<string, int> nodeIndexById;

	private Dictionary<Vector2Int, int> nodeIndexByPos;

	public TreeGraph(string id, TreeGraphNode<T>[] nodes, Vector2Int size, string defaultNodeName)
	{
		this.id = id;
		this.size = size;
		this.nodes = nodes;
		nodeIndexById = BuildNodeIdxDictByName(nodes);
		nodeIndexByPos = BuildNodeIdxDictByPos(nodes);
		links = BuildLinks(nodes, nodeIndexById);
		if (defaultNodeName == null)
		{
			defaultNodeName = string.Empty;
		}
		int valueOrDefault = nodeIndexById.GetValueOrDefault(defaultNodeName, 0);
		defaultNode = nodes[valueOrDefault];
		defaultPos = defaultNode.pos;
	}

	private Dictionary<string, int> BuildNodeIdxDictByName(TreeGraphNode<T>[] nodes)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < nodes.Length; i++)
		{
			dictionary.Add(nodes[i].id, i);
		}
		return dictionary;
	}

	private Dictionary<Vector2Int, int> BuildNodeIdxDictByPos(TreeGraphNode<T>[] ndoes)
	{
		Dictionary<Vector2Int, int> dictionary = new Dictionary<Vector2Int, int>();
		for (int i = 0; i < nodes.Length; i++)
		{
			dictionary.Add(nodes[i].pos, i);
		}
		return dictionary;
	}

	private TreeGraphLink[] BuildLinks(TreeGraphNode<T>[] nodes, Dictionary<string, int> indexMap)
	{
		List<TreeGraphLink> list = new List<TreeGraphLink>();
		foreach (TreeGraphNode<T> treeGraphNode in nodes)
		{
			if (treeGraphNode.parents.Length != 0)
			{
				string[] parents = treeGraphNode.parents;
				foreach (string key in parents)
				{
					list.Add(new TreeGraphLink(nodes[indexMap[key]].pos, treeGraphNode.pos));
				}
			}
		}
		return list.ToArray();
	}

	public bool QueryNode(string id, out TreeGraphNode<T> node)
	{
		if (nodeIndexById.ContainsKey(id))
		{
			node = nodes[nodeIndexById[id]];
			return true;
		}
		node = null;
		return false;
	}

	public bool QueryNode(Vector2Int pos, out TreeGraphNode<T> node)
	{
		if (nodeIndexByPos.ContainsKey(pos))
		{
			node = nodes[nodeIndexByPos[pos]];
			return true;
		}
		node = null;
		return false;
	}

	public TreeGraphNode<T> GetNode(string id)
	{
		if (nodeIndexById.ContainsKey(id))
		{
			return nodes[nodeIndexById[id]];
		}
		return null;
	}

	public TreeGraphNode<T> GetNode(Vector2Int pos)
	{
		if (nodeIndexByPos.ContainsKey(pos))
		{
			return nodes[nodeIndexByPos[pos]];
		}
		return null;
	}

	public bool Contains(Vector2Int pos)
	{
		return nodeIndexByPos.ContainsKey(pos);
	}

	public bool Contains(string id)
	{
		return nodeIndexById.ContainsKey(id);
	}

	public void ForEach(Action<TreeGraphNode<T>> handle)
	{
		TreeGraphNode<T>[] array = nodes;
		foreach (TreeGraphNode<T> obj in array)
		{
			handle(obj);
		}
	}
}
