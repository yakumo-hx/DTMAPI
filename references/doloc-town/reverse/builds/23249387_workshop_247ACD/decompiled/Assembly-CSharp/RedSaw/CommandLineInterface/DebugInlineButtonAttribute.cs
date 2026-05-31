using System;
using UnityEngine;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class DebugInlineButtonAttribute : Attribute
{
	private const int MIN_WIDTH = 50;

	public readonly string title;

	public readonly string callbackName;

	private int _maxWidth = 50;

	public int MaxWidth
	{
		get
		{
			return _maxWidth;
		}
		set
		{
			_maxWidth = Mathf.Max(50, value);
		}
	}

	public DebugInlineButtonAttribute(string title, string callbackName)
	{
		this.title = title;
		this.callbackName = callbackName;
	}

	public DebugInlineButtonAttribute(string callbackName)
	{
		title = null;
		this.callbackName = callbackName;
	}
}
