using System;
using System.Collections.Generic;
using DolocTown.Config.TechTree;
using DolocTown.TreeGraph;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.GameData;

[Name("科技节点", 0)]
[Description("科技树的节点,用该节点来表示科技树中的科技")]
public class TechTreeNode : TechTreeNodeGroup
{
	public enum UnlockType
	{
		Equipment,
		Building,
		Recipe
	}

	[Serializable]
	public struct UnlockInfo
	{
		public UnlockType unlockType;

		public string unlockInfo;

		public override readonly string ToString()
		{
			return unlockType switch
			{
				UnlockType.Equipment => "+ 设备<" + unlockInfo + ">", 
				UnlockType.Building => "+ 建筑<" + unlockInfo + ">", 
				UnlockType.Recipe => "+ 配方<" + unlockInfo + ">", 
				_ => string.Empty, 
			};
		}
	}

	[Serializable]
	public struct TechPointCost
	{
		public TechPointType type;

		public int count;

		public readonly TechNodeCost Read()
		{
			return new TechNodeCost(type, count);
		}

		public override readonly string ToString()
		{
			return type switch
			{
				TechPointType.ANIMAL => $"动物点数x{count}", 
				TechPointType.OPERATE => $"操作点数x{count}", 
				TechPointType.SCIENCE => $"科学点数x{count}", 
				TechPointType.NATURE => $"自然点数x{count}", 
				_ => $"未知点数x{count}", 
			};
		}
	}

	public static Color defaultColor = new Color(0.76f, 0.9f, 0.95f);

	public static Color errorColor = new Color(1f, 0.25f, 0.25f);

	private bool isUnlockFoldout;

	private bool isCostFoldout;

	private Queue<int> removeIndexQueue = new Queue<int>();

	[SerializeField]
	public string _techName;

	[SerializeField]
	public Sprite techIcon;

	[SerializeField]
	public List<UnlockInfo> unlockInfos = new List<UnlockInfo>();

	[SerializeField]
	public List<TechPointCost> costList = new List<TechPointCost>();

	[SerializeField]
	public bool isNodeUseful = true;

	public override string name
	{
		get
		{
			if (_techName == null)
			{
				return $"未设置节点Id/{base.comments}/{treeCanvasPos}";
			}
			return $"{_techName}/{base.comments}/{treeCanvasPos}";
		}
	}

	public override TreeGraphNode<TechNodeProto> CreateNode(int maxYIndex, Sprite defaultIcon)
	{
		string[] equipments = new string[0];
		string[] buildings = new string[0];
		string[] recipes = new string[0];
		if (unlockInfos != null && unlockInfos.Count != 0)
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			foreach (UnlockInfo unlockInfo in unlockInfos)
			{
				switch (unlockInfo.unlockType)
				{
				case UnlockType.Equipment:
					if (!unlockInfo.unlockInfo.IsNullOrEmpty())
					{
						list.Add(unlockInfo.unlockInfo);
					}
					break;
				case UnlockType.Building:
					if (!unlockInfo.unlockInfo.IsNullOrEmpty())
					{
						list2.Add(unlockInfo.unlockInfo);
					}
					break;
				case UnlockType.Recipe:
					if (!unlockInfo.unlockInfo.IsNullOrEmpty())
					{
						list3.Add(unlockInfo.unlockInfo);
					}
					break;
				}
			}
			equipments = list.ToArray();
			buildings = list2.ToArray();
			recipes = list3.ToArray();
		}
		TechNodeCost[] costs = new TechNodeCost[0];
		if (costList != null)
		{
			List<TechNodeCost> list4 = new List<TechNodeCost>();
			foreach (TechPointCost cost in costList)
			{
				list4.Add(cost.Read());
			}
			costs = list4.ToArray();
		}
		string[] parents = new string[0];
		if (base.inConnections.Count != 0)
		{
			List<string> list5 = new List<string>();
			foreach (Node parentNode in GetParentNodes())
			{
				if (parentNode is TechTreeNode techTreeNode)
				{
					list5.Add(techTreeNode._techName);
				}
			}
			parents = list5.ToArray();
		}
		Sprite icon = techIcon ?? defaultIcon;
		TechNodeProto data = new TechNodeProto(_techName, equipments, buildings, recipes, costs, icon, isNodeUseful);
		return new TreeGraphNode<TechNodeProto>(_techName, treeCanvasPos, parents, data);
	}
}
