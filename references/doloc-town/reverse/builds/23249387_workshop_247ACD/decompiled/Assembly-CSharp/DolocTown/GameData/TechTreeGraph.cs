using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇[农场]/科技树")]
public class TechTreeGraph : TreeGraphSO<TechTreeNodeGroup, TechTreeNode, TechNodeProto>
{
	private string[] _availableNodeNames;

	public override string[] AvailableNodeNames
	{
		get
		{
			if (_availableNodeNames == null)
			{
				_availableNodeNames = GetAvailableNodeNames();
			}
			return _availableNodeNames;
		}
	}

	public void UpdateAvailableNodeNames()
	{
		_availableNodeNames = GetAvailableNodeNames();
	}

	private string[] GetAvailableNodeNames()
	{
		IEnumerable<TechTreeNode> source = base.allNodes.OfType<TechTreeNode>();
		_availableNodeNames = source.Select((TechTreeNode node) => node._techName).ToArray();
		return _availableNodeNames;
	}
}
