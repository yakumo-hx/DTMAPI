using System;
using System.Collections.Generic;
using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.GameData;

[GraphInfo(packageName = "NodeCanvas", docsURL = "https://nodecanvas.paradoxnotion.com/documentation/", resourcesURL = "https://nodecanvas.paradoxnotion.com/downloads/", forumsURL = "https://nodecanvas.paradoxnotion.com/forums-page/")]
[CreateAssetMenu(menuName = "多洛可小镇/事务计划表")]
public class RoutineGraph : Graph, IMessageReceiver
{
	private RoutineNode_Trigger[] _triggerNodes;

	public override Type baseNodeType => typeof(RoutineNode);

	public override bool requiresAgent => false;

	public override bool requiresPrimeNode => true;

	public override bool isTree => false;

	public override bool allowBlackboardOverrides => false;

	public override bool canAcceptVariableDrops => false;

	public void SendMessage(GameMessage message)
	{
		if (_triggerNodes == null)
		{
			if (base.primeNode == null || base.primeNode.outConnections.Count == 0)
			{
				_triggerNodes = Array.Empty<RoutineNode_Trigger>();
				return;
			}
			List<RoutineNode_Trigger> list = new List<RoutineNode_Trigger>();
			foreach (Connection outConnection in base.primeNode.outConnections)
			{
				if (outConnection.targetNode is RoutineNode_Trigger item)
				{
					list.Add(item);
				}
			}
			_triggerNodes = list.ToArray();
		}
		if (_triggerNodes.Length != 0)
		{
			RoutineNode_Trigger[] triggerNodes = _triggerNodes;
			for (int i = 0; i < triggerNodes.Length; i++)
			{
				triggerNodes[i].SendMessage(message);
			}
		}
	}
}
