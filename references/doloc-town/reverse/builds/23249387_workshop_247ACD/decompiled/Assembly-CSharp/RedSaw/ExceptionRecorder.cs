using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public class ExceptionRecorder
{
	private readonly ExceptionAnalyzer analyzer = new ExceptionAnalyzer();

	private readonly HashSet<ExceptionTriggerPoint> ignoreExceptions = new HashSet<ExceptionTriggerPoint>();

	[JsonProperty]
	private readonly int maxReactionTimes;

	[JsonProperty]
	private readonly Dictionary<string, int> exceptionReactionCount = new Dictionary<string, int>();

	[JsonProperty]
	private readonly HashSet<ExceptionTriggerPoint> exceptions = new HashSet<ExceptionTriggerPoint>();

	[Command("bug_test", Desc = "测试异常记录器")]
	public static void BugTest()
	{
		Debug.LogException(new Exception("This is a test exception for Bug Reporter"));
	}

	[Command("bug_test2", Desc = "测试异常记录器")]
	public static void BugTest2()
	{
		Debug.LogException(new ArgumentException("This is another bug test"));
	}

	public ExceptionRecorder(int maxReactionTimes = 3)
	{
		this.maxReactionTimes = maxReactionTimes;
	}

	[JsonConstructor]
	public ExceptionRecorder(int maxReactionTimes, Dictionary<string, int> exceptionReactionCount, HashSet<ExceptionTriggerPoint> exceptions)
	{
		this.maxReactionTimes = maxReactionTimes;
		this.exceptionReactionCount = exceptionReactionCount;
		this.exceptions = exceptions;
	}

	public void AddIgnoreCase(ExceptionTriggerPoint point)
	{
		ignoreExceptions.Add(point);
	}

	public bool Record(string message, string stackTrace, out ExceptionAnalyzeResult result)
	{
		result = default(ExceptionAnalyzeResult);
		if (string.IsNullOrEmpty(message))
		{
			return false;
		}
		if (exceptionReactionCount.TryGetValue(message, out var value) && value > maxReactionTimes)
		{
			return false;
		}
		exceptionReactionCount[message] = value + 1;
		result = ExceptionAnalyzer.Analyze(message, stackTrace);
		ExceptionTriggerPoint finalTriggerPoint = result.FinalTriggerPoint;
		if (finalTriggerPoint.IsEmpty)
		{
			return false;
		}
		if (!ignoreExceptions.Contains(finalTriggerPoint))
		{
			return exceptions.Add(finalTriggerPoint);
		}
		return false;
	}

	public bool DebugRecord(string mesage, string stackTrace, out ExceptionAnalyzeResult result)
	{
		result = default(ExceptionAnalyzeResult);
		if (string.IsNullOrEmpty(mesage))
		{
			return false;
		}
		result = ExceptionAnalyzer.Analyze(mesage, stackTrace);
		if (result.FinalTriggerPoint.IsEmpty)
		{
			return false;
		}
		return true;
	}
}
