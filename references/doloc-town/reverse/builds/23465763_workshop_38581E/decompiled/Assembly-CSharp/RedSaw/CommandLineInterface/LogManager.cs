using System;
using System.Collections.Generic;
using System.Linq;

namespace RedSaw.CommandLineInterface;

public class LogManager<T> where T : Enum
{
	public readonly struct Log
	{
		public readonly string message;

		public readonly string color;

		public readonly T logType;

		public static Log Empty => new Log(string.Empty);

		public bool HasColor
		{
			get
			{
				if (color != null)
				{
					return color.Length > 0;
				}
				return false;
			}
		}

		public Log(string message, string color, T logType = default(T))
		{
			this.message = message;
			this.color = color;
			this.logType = logType;
		}

		public Log(string message, T logType = default(T))
		{
			this.message = message;
			color = null;
			this.logType = logType;
		}

		public override string ToString()
		{
			if (color == null)
			{
				return message;
			}
			return "<color=" + color + ">" + message + "</color>";
		}
	}

	private readonly int logCapacity;

	private readonly bool hasLogCapacity;

	private readonly List<Log> logs = new List<Log>();

	public IEnumerable<(string, string)> allLogs => logs.Select((Log x) => (message: x.message, color: x.color));

	public IEnumerable<Log> AllLogs => logs;

	public event Action<Log> OnReceivedMessage;

	public LogManager(int logCapacity = -1)
	{
		this.logCapacity = logCapacity;
		hasLogCapacity = logCapacity > 0;
	}

	public void Output(string info, T type = default(T))
	{
		if (info == null || info.Length == 0)
		{
			Output(Log.Empty);
		}
		else
		{
			Output(new Log(info, type));
		}
	}

	public void Output(string info, string color, T type = default(T))
	{
		if (color == null || color.Length == 0)
		{
			Output(info, type);
		}
		else if (info == null || info.Length == 0)
		{
			Output(Log.Empty);
		}
		else
		{
			Output(new Log(info, color, type));
		}
	}

	private void Output(Log log)
	{
		logs.Add(log);
		if (hasLogCapacity && logs.Count >= logCapacity)
		{
			logs.RemoveAt(0);
		}
		this.OnReceivedMessage?.Invoke(log);
	}

	public IEnumerable<Log> GetLastLogs(int count)
	{
		if (count != 0)
		{
			count = Math.Min(count, logs.Count);
			int num = logs.Count - count;
			int R = num + count;
			for (int i = num; i < R; i++)
			{
				yield return logs[i];
			}
		}
	}

	public IEnumerable<Log> GetLastLogs(int count, T type = default(T))
	{
		if (count == 0)
		{
			yield break;
		}
		foreach (Log log in logs)
		{
			if (log.logType.Equals(type))
			{
				yield return log;
			}
			int num = count - 1;
			count = num;
			if (num <= 0)
			{
				break;
			}
		}
	}
}
