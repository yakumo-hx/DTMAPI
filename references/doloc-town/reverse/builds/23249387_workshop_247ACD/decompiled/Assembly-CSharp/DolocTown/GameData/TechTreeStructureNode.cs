using DolocTown.TreeGraph;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.GameData;

[Name("结构节点", 0)]
[Description("用于占据一个canvas的位置,没有任何功能")]
[Color("ff7f50")]
public class TechTreeStructureNode : TechTreeNodeGroup
{
	public override TreeGraphNode<TechNodeProto> CreateNode(int maxYIndex, Sprite defaultIcon)
	{
		return null;
	}
}
