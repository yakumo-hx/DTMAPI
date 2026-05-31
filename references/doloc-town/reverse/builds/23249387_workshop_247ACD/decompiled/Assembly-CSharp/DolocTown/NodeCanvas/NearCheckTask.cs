using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("距离检测", 0)]
[Description("检测两个位置之间的距离是否小于等于一个阈值")]
public class NearCheckTask : ConditionTask
{
	[RequiredField]
	[ExposeField]
	public BBParameter<Vector2> leftPoint = new BBParameter<Vector2>();

	[RequiredField]
	[ExposeField]
	public BBParameter<Vector2> rightPoint = new BBParameter<Vector2>();

	[RequiredField]
	[ExposeField]
	public float nearThreshold = 3f;

	protected override string info => "临近";

	protected override bool OnCheck()
	{
		return Vector2.Distance(leftPoint.value, rightPoint.value) <= nearThreshold;
	}
}
