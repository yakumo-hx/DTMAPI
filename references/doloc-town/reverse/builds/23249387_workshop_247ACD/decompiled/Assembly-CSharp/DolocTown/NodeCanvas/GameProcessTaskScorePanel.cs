using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("计分板", 0)]
[Description("部署一个监听任务，每条监听消息都会触发计分")]
public class GameProcessTaskScorePanel : GameProcessTask, IGameProcessMission
{
	[Serializable]
	private struct ScoreCounter
	{
		[SerializeField]
		public string gameEventTypeStr;

		[SerializeField]
		public bool hasArgs;

		[SerializeField]
		public string args;

		[SerializeField]
		public int score;

		public bool Check(GameEventType type, string args)
		{
			if (hasArgs)
			{
				if (gameEventTypeStr == type.ToString())
				{
					return args == this.args;
				}
				return false;
			}
			return gameEventTypeStr == type.ToString();
		}
	}

	[SerializeField]
	private List<ScoreCounter> _scoreCounters = new List<ScoreCounter>();

	[SerializeField]
	private bool customFormat;

	[SerializeField]
	private string _resultFormatStr;

	[SerializeField]
	private float delayDuration;

	private int score;

	public GameProcessTaskScorePanel(Graph _graph)
		: base(_graph)
	{
	}

	public bool SendMessage(GameMessage message, bool shouldUseMessage = true)
	{
		GameEventArgs args = message.Args;
		GameEventArgsString argsStr = args as GameEventArgsString;
		if (argsStr == null)
		{
			return false;
		}
		foreach (ScoreCounter item in _scoreCounters.Where((ScoreCounter scoreCounter) => scoreCounter.Check(message.Type, argsStr.value)))
		{
			score += item.score;
		}
		return false;
	}

	public override TaskStatus OnExecute(float dt)
	{
		return TaskStatus.Executing;
	}

	public override void OnBegin()
	{
		base.OnBegin();
		score = 0;
	}

	public override void OnEnd()
	{
		base.OnEnd();
		string content = (customFormat ? _resultFormatStr : DolocConfig.GetL10nText(_resultFormatStr));
		if (content.IsNullOrEmpty())
		{
			content = "missing result text {0}";
		}
		DolocAPI.Delay(delayDuration, delegate
		{
			DolocAPI.ShowMessageBoxAttention(content.Format(score));
		});
	}
}
