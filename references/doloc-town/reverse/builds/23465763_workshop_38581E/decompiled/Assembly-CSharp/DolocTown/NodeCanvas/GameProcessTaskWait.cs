using DolocTown.UI;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("等待", 0)]
[Description("等待一段时间后状态结束")]
public class GameProcessTaskWait : GameProcessTask
{
	[SerializeField]
	private float _waitTime;

	[SerializeField]
	private bool _showTimer;

	private float _currentTimer;

	private TimerTip _timerTip;

	public override string SummaryInfo => $"等待 <color=#fffde3>{_waitTime}</color> 秒";

	public override string ExecutionInfo => $"剩余 <color=#ffff00><size=11>{_currentTimer:0.00}</size></color>s";

	public GameProcessTaskWait(Graph graph)
		: base(graph)
	{
	}

	public override void OnBegin()
	{
		base.OnBegin();
		_currentTimer = _waitTime;
		if (_showTimer)
		{
			_timerTip = DolocAPI.uiSystem.GetEntity<TimerTip>();
			if (_timerTip == null)
			{
				Debug.LogWarning("未找到计时器UI");
			}
			else
			{
				_timerTip.Show();
			}
		}
	}

	public override TaskStatus OnExecute(float dt)
	{
		_currentTimer = Mathf.Max(_currentTimer - dt, 0f);
		if (_showTimer && _timerTip != null)
		{
			_timerTip.Text = $"{_currentTimer:0.00}s";
		}
		if (!(_currentTimer <= 0f))
		{
			return TaskStatus.Executing;
		}
		return TaskStatus.Success;
	}

	public override void OnEnd()
	{
		base.OnEnd();
		if (_showTimer && _timerTip != null)
		{
			_timerTip.Hide();
			_timerTip = null;
		}
	}
}
