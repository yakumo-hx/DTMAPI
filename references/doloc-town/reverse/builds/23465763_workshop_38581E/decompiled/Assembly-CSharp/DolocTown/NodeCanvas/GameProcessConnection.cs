using NodeCanvas.Framework;
using UnityEngine;

namespace DolocTown.NodeCanvas;

public class GameProcessConnection : HorizontalLinkedConnection
{
	[SerializeField]
	private GameProcessTask _conditionTask;

	private bool isUnfolded;

	public GameProcessTask ConditionTask => _conditionTask;

	public string ConnectionLabel
	{
		get
		{
			if (_conditionTask == null)
			{
				if (base.status != Status.Resting)
				{
					return "等待主节点完成..";
				}
				return "完成后";
			}
			if (base.status != Status.Resting)
			{
				return "失败条件\n" + _conditionTask.SummaryInfo + "\n" + _conditionTask.ExecutionInfo;
			}
			return "失败条件\n" + _conditionTask.SummaryInfo;
		}
	}
}
