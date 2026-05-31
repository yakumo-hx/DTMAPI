using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DebugProgressBarAttribute : Attribute
{
	public readonly string textGetter;

	public float maxValue { get; set; } = 1f;


	public float minValue { get; set; }

	public int maxValueInt { get; set; } = 100;


	public int minValueInt { get; set; }

	public DebugProgressBarAttribute(string textGetter = null)
	{
		this.textGetter = textGetter;
	}

	public float GetProgress(float value)
	{
		if (maxValue <= minValue)
		{
			return 0f;
		}
		return (value - minValue) / (maxValue - minValue);
	}

	public float GetProgress(int value)
	{
		if (maxValueInt <= minValueInt)
		{
			return 0f;
		}
		return (float)(value - minValueInt) / (float)(maxValueInt - minValueInt);
	}
}
