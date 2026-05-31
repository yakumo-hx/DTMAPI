using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/Utils")]
[Name("等待", 0)]
[Description("等待一段时间")]
public class Wait : ActionTask
{
	[ExposeField]
	public bool hasRange;

	[ExposeField]
	[ShowIf("hasRange", 0)]
	public BBParameter<float> waitTime = 1f;

	[ExposeField]
	[ShowIf("hasRange", 1)]
	public BBParameter<float> minWaitTime = 1f;

	[ExposeField]
	[ShowIf("hasRange", 1)]
	public BBParameter<float> maxWaitTime = 3f;

	private float _waitTime;

	protected override string info
	{
		get
		{
			if (hasRange)
			{
				return "等待" + minWaitTime?.ToString() + "到" + maxWaitTime?.ToString() + "秒";
			}
			return $"等待{waitTime}秒";
		}
	}

	protected override void OnExecute()
	{
		if (hasRange)
		{
			_waitTime = Random.Range(minWaitTime.value, maxWaitTime.value);
		}
		else
		{
			_waitTime = waitTime.value;
		}
	}

	protected override void OnUpdate()
	{
		if (base.elapsedTime >= _waitTime)
		{
			EndAction(success: true);
		}
	}
}
