using System;
using System.Reflection;
using UnityEngine;

namespace RedSaw.CommandLineInterface;

public abstract class DebugField
{
	public readonly object instance;

	public readonly MemberInfo memberInfo;

	public readonly DebugOnValueChangedAttribute onValueChangedAttribute;

	public readonly DebugProgressBarAttribute progressBarAttribute;

	public readonly DebugInlineButtonAttribute InlineButtonAttributeAttr;

	public readonly DebugButton inlineButton;

	private MethodInfo _callbackMethod;

	private bool _isCallbackMethodLoaded;

	private bool _isCallbackMethodValid;

	public string Name => memberInfo.Name;

	public abstract Type FieldType { get; }

	public abstract bool IsReadonly { get; }

	public abstract object Value { get; }

	protected DebugField(object instance, MemberInfo memberInfo)
	{
		this.instance = instance;
		this.memberInfo = memberInfo;
		onValueChangedAttribute = memberInfo.GetCustomAttribute<DebugOnValueChangedAttribute>();
		progressBarAttribute = memberInfo.GetCustomAttribute<DebugProgressBarAttribute>();
		InlineButtonAttributeAttr = memberInfo.GetCustomAttribute<DebugInlineButtonAttribute>();
		if (InlineButtonAttributeAttr != null)
		{
			MethodInfo method = instance.GetType().GetMethod(InlineButtonAttributeAttr.callbackName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				string title = InlineButtonAttributeAttr.title ?? method.Name;
				inlineButton = new DebugButton(instance, method, title, string.Empty, DebugButtonType.Inline);
			}
		}
	}

	public abstract bool TryGetValue<T>(out T value);

	public abstract void TrySetValue<T>(T value);

	protected void OnValueChanged<T>(T newValue)
	{
		if (onValueChangedAttribute == null)
		{
			return;
		}
		if (_isCallbackMethodLoaded)
		{
			if (_isCallbackMethodValid)
			{
				_OnValueChanged(newValue, _callbackMethod);
			}
			return;
		}
		_isCallbackMethodLoaded = true;
		_isCallbackMethodValid = false;
		string callbackName = onValueChangedAttribute.callbackName;
		_callbackMethod = instance.GetType().GetMethod(callbackName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		if (_callbackMethod == null)
		{
			Debug.LogError("DebugFieldOfFieldInfo: Method " + callbackName + " not found in " + instance.GetType());
			return;
		}
		ParameterInfo[] parameters = _callbackMethod.GetParameters();
		if (parameters.Length > 1)
		{
			Debug.LogError("DebugFieldOfFieldInfo: Method " + callbackName + " has too many parameters in " + instance.GetType());
		}
		else if (parameters.Length == 1 && !parameters[0].ParameterType.IsAssignableFrom(FieldType))
		{
			Debug.LogError("DebugFieldOfFieldInfo: Method " + callbackName + " parameter type mismatch in " + instance.GetType()?.ToString() + ", expected " + FieldType?.ToString() + ", but got " + parameters[0].ParameterType);
		}
		else
		{
			_isCallbackMethodValid = true;
			_OnValueChanged(newValue, _callbackMethod);
		}
	}

	private void _OnValueChanged<T>(T newValue, MethodInfo methodInfo)
	{
		object obj = (methodInfo.IsStatic ? null : instance);
		methodInfo.Invoke(obj, (methodInfo.GetParameters().Length != 1) ? null : new object[1] { newValue });
	}
}
