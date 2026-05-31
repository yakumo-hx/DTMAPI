using System;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown.GameData;

public class MissionRequire_Simple : MissionRequire
{
	public class Handle : MissionRequireHandle
	{
		private readonly MissionRequire_Simple _simple;

		[JsonProperty]
		private int count;

		public override bool IsInvalid => false;

		public override bool IsComplete => count >= _simple.count;

		public override bool IsCompleteLoadArchive => IsComplete;

		public override bool IsCompleteBeforeInit => false;

		public Handle(MissionRequire_Simple require)
			: base(require)
		{
			_simple = require;
			count = 0;
		}

		[JsonConstructor]
		public Handle(MissionRequire_Simple require, int count)
			: base(require)
		{
			_simple = require;
			this.count = count;
		}

		public override bool SendMessage(GameEventType eventType, GameEventArgs args, out bool statusChanged)
		{
			statusChanged = false;
			if (eventType != _simple.eventType)
			{
				return false;
			}
			if (_simple.args != null)
			{
				if (!(args is GameEventArgsString gameEventArgsString))
				{
					return false;
				}
				if (gameEventArgsString.value != _simple.args)
				{
					return false;
				}
			}
			statusChanged = true;
			return ++count >= _simple.count;
		}

		public override bool TryInvokeHistoryInSandBox(out string reason)
		{
			reason = "不支持对简单任务进行历史重建";
			return false;
		}

		public override void ClearProgress()
		{
			count = 0;
		}

		public override string ToString()
		{
			if (_simple.eventType == GameEventType.COMPLETE_DIALOGUE)
			{
				return "";
			}
			Color color = ((count >= _simple.count) ? DolocUiColor.SLIENTCOLOR_GREEN : DolocUiColor.EYECATCHCOLOR_CYAN);
			return $"{count}/{_simple.count}".Colored(color);
		}
	}

	private readonly GameEventType eventType;

	[JsonProperty]
	private readonly int count;

	[JsonProperty]
	private readonly string args;

	[JsonProperty]
	private string eventTypeStr => eventType.ToString();

	[JsonConstructor]
	public MissionRequire_Simple(string eventTypeStr, int count, string args)
	{
		Enum.TryParse<GameEventType>(eventTypeStr, ignoreCase: true, out eventType);
		this.count = count;
		this.args = args;
	}

	public override MissionRequireHandle CreateHandle()
	{
		return new Handle(this);
	}
}
