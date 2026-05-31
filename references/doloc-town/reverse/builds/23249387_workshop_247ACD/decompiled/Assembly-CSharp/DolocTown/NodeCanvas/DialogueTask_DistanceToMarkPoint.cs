using DolocTown.Config;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查与标记点的距离", 0)]
[Description("检查主角与给定的标记点的距离是否小于指定值")]
public class DialogueTask_DistanceToMarkPoint : DialogueConditionTask
{
	private enum DistanceType
	{
		Horizontal,
		Vertical,
		Distance,
		Manhatten,
		Area
	}

	[SerializeField]
	private string markPointName = string.Empty;

	[SerializeField]
	private DistanceType distanceType = DistanceType.Distance;

	[SerializeField]
	private float distance = 3f;

	[SerializeField]
	private Vector2 areaSize = Vector2.zero;

	[SerializeField]
	private bool offsetMarkPoint;

	[SerializeField]
	private Vector2 offset = Vector2.zero;

	private string typeName => distanceType switch
	{
		DistanceType.Horizontal => "水平距离", 
		DistanceType.Vertical => "垂直距离", 
		DistanceType.Distance => "距离", 
		DistanceType.Manhatten => "曼哈顿距离", 
		DistanceType.Area => "区域", 
		_ => "距离", 
	};

	public override string taskTitle
	{
		get
		{
			if (distanceType == DistanceType.Area)
			{
				return $"主角与标记点\"{markPointName}\"的{typeName}在{areaSize}范围内";
			}
			return $"与标记点\"{markPointName}\"的{typeName}小于{distance}";
		}
	}

	protected override bool CheckCondition()
	{
		if (markPointName.IsNullOrEmpty())
		{
			return false;
		}
		if (!DolocConfig.Tables.TbMarkPoint.DataMap.TryGetValue(markPointName, out var value))
		{
			return false;
		}
		Vector2 vector = (offsetMarkPoint ? (value.Position + offset) : value.Position);
		if (DolocAPI.CurrentRoom == null)
		{
			return false;
		}
		if (DolocAPI.CurrentRoom.SceneRawName != value.SceneRawName)
		{
			return false;
		}
		switch (distanceType)
		{
		case DistanceType.Distance:
			return Vector3.Distance(DolocAPI.AgentPosition, vector) <= distance;
		case DistanceType.Horizontal:
			return Mathf.Abs(DolocAPI.AgentPosition.x - vector.x) <= distance;
		case DistanceType.Vertical:
			return Mathf.Abs(DolocAPI.AgentPosition.y - vector.y) <= distance;
		case DistanceType.Manhatten:
			return Mathf.Abs(DolocAPI.AgentPosition.x - vector.x) + Mathf.Abs(DolocAPI.AgentPosition.y - vector.y) <= distance;
		case DistanceType.Area:
			if (Mathf.Abs(DolocAPI.AgentPosition.x - vector.x) <= areaSize.x)
			{
				return Mathf.Abs(DolocAPI.AgentPosition.y - vector.y) <= areaSize.y;
			}
			return false;
		default:
			return Vector3.Distance(DolocAPI.AgentPosition, vector) <= distance;
		}
	}
}
