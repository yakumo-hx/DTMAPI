using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DolocTown;

namespace RedSaw;

public class ExceptionAnalyzer
{
	public static ExceptionAnalyzeResult Analyze(string message, string stackTrace)
	{
		(string, string) tuple = AnalyzeMessage(message);
		string item = tuple.Item1;
		string item2 = tuple.Item2;
		ExceptionTriggerPoint[] triggerPoints = AnalyzeStackTrace(stackTrace);
		return new ExceptionAnalyzeResult(item, item2, stackTrace, triggerPoints);
	}

	private static (string, string) AnalyzeMessage(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return (string.Empty, string.Empty);
		}
		message = message.Trim();
		if (!message.Contains(":"))
		{
			return (string.Empty, message);
		}
		string[] array = message.Split(':', 2);
		if (array.Length != 2)
		{
			return (string.Empty, message);
		}
		return (array[0].Trim(), array[1].Trim());
	}

	private static ExceptionTriggerPoint[] AnalyzeStackTrace(string stackTrace)
	{
		if (string.IsNullOrEmpty(stackTrace))
		{
			return Array.Empty<ExceptionTriggerPoint>();
		}
		string[] array = stackTrace.Split('\n');
		List<ExceptionTriggerPoint> list = new List<ExceptionTriggerPoint>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!text.IsNullOrEmpty())
			{
				list.Add(_AnalyzeTriggerPoint(text));
			}
		}
		return list.ToArray();
	}

	private static ExceptionTriggerPoint _AnalyzeTriggerPoint(string line)
	{
		Match match = Regex.Match(line, "^(.*?)\\s\\(at\\s(.*?):(\\d+)\\)$");
		if (match.Success)
		{
			string value = match.Groups[1].Value;
			string value2 = match.Groups[2].Value;
			int line2 = int.Parse(match.Groups[3].Value);
			return new ExceptionTriggerPoint(value, value2, line2);
		}
		return new ExceptionTriggerPoint(line);
	}
}
