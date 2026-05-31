using System;
using System.Collections.Generic;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class VirtualMachine
{
	public enum CallTarget
	{
		Unset,
		RequireCallable,
		RequireValue
	}

	public struct ExecuteStatus
	{
		public Type requiredType;

		public CallTarget target;

		public ExecuteStatus(Type requiredType, CallTarget target)
		{
			this.target = target;
			this.requiredType = requiredType;
		}
	}

	private readonly bool IgnoreInvalidSetBehaviour;

	private readonly bool TreatVoidAsNull;

	private readonly bool ReceiveValueFromNonStringType;

	private readonly Stack<StackObject> _stack = new Stack<StackObject>();

	private readonly ValueParserManager parsersDefault = new ValueParserManager(DefaultTypeParserDefination.DefaultTypeParser, DefaultTypeParserDefination.DefaultTypeAlias);

	private readonly ValueParserManager parsersCustom = new ValueParserManager();

	private readonly Dictionary<string, StackProperty> properties = new Dictionary<string, StackProperty>();

	private readonly Dictionary<string, StackPropertyCustom> localVariables = new Dictionary<string, StackPropertyCustom>();

	private readonly Dictionary<string, StackCallable> callables = new Dictionary<string, StackCallable>();

	public IEnumerable<StackCallable> AllCallables => callables.Values;

	public IEnumerable<StackProperty> AllProperties
	{
		get
		{
			foreach (StackProperty value in properties.Values)
			{
				yield return value;
			}
			foreach (StackPropertyCustom value2 in localVariables.Values)
			{
				yield return value2;
			}
		}
	}

	public StackObject TopValue
	{
		get
		{
			if (_stack.Count > 0)
			{
				return _stack.Pop();
			}
			throw new CommandExecuteException("fatal stack error");
		}
	}

	public VirtualMachine(bool IgnoreInvalidSetBehaviour = true, bool ReceiveValueFromNonStringType = true, bool TreatVoidAsNull = true)
	{
		this.IgnoreInvalidSetBehaviour = IgnoreInvalidSetBehaviour;
		this.ReceiveValueFromNonStringType = ReceiveValueFromNonStringType;
		this.TreatVoidAsNull = TreatVoidAsNull;
	}

	public void RegisterValueParser(Type type, ValueParser parser, string typeAlias = null)
	{
		parsersCustom.RegisterParser(type, parser, typeAlias);
	}

	public Delegate GetDelegate(string key)
	{
		if (callables.ContainsKey(key))
		{
			return callables[key].GetDelegate();
		}
		return null;
	}

	public IEnumerable<(string, Delegate)> GetAllDelegate()
	{
		foreach (string key in callables.Keys)
		{
			yield return (key, callables[key].GetDelegate());
		}
	}

	public bool RegisterCallable(StackCallable callable, bool shouldOverwrite = true)
	{
		if (callables.ContainsKey(callable.Name))
		{
			if (shouldOverwrite)
			{
				callables[callable.Name] = callable;
				return true;
			}
			return false;
		}
		callables.Add(callable.Name, callable);
		return true;
	}

	public bool RegisterProperty(StackProperty property, bool shouldOverwrite = true)
	{
		if (properties.ContainsKey(property.Name))
		{
			if (shouldOverwrite)
			{
				properties[property.Name] = property;
				return true;
			}
			return false;
		}
		properties.Add(property.Name, property);
		return true;
	}

	public object GetLocalVariable(string name)
	{
		if (localVariables.TryGetValue(name, out var value))
		{
			return value.GetValue();
		}
		return null;
	}

	public void SetLocalVariable(string name, object value)
	{
		if (localVariables.TryGetValue(name, out var value2))
		{
			value2.SetValue(value);
			return;
		}
		StackPropertyCustom value3 = new StackPropertyCustom(name, value);
		localVariables.Add(name, value3);
	}

	public Type GetPropertyType(string propertyName)
	{
		if (localVariables.TryGetValue(propertyName, out var value))
		{
			return value.ValueType;
		}
		if (properties.TryGetValue(propertyName, out var value2))
		{
			return value2.ValueType;
		}
		return null;
	}

	public Type GetCallableType(string name)
	{
		if (callables.TryGetValue(name, out var value))
		{
			return value.Instance?.GetType();
		}
		return null;
	}

	private void SetProperty(IValueSetter propertyHandle, object value, string DebugInfo)
	{
		if (propertyHandle is StackPropertyCustom stackPropertyCustom)
		{
			stackPropertyCustom.SetValue(value);
			return;
		}
		if (value == null)
		{
			if (propertyHandle.ValueType.IsNullable())
			{
				propertyHandle.SetValue(null);
				return;
			}
			throw new CommandExecuteException("cannot assign \"null\" to \"" + propertyHandle.Name + "\" near \"" + DebugInfo + "\"");
		}
		Type type = value.GetType();
		if (propertyHandle.ValueType.IsAssignableFrom(type))
		{
			propertyHandle.SetValue(value);
			return;
		}
		if (propertyHandle.ValueType == typeof(string))
		{
			if (ReceiveValueFromNonStringType)
			{
				propertyHandle.SetValue(value.ToString());
				return;
			}
			throw new CommandExecuteException($"cannot assign \"{value}({value.GetType()})\" to \"{propertyHandle.Name} (string)\" near \"{DebugInfo}\"");
		}
		if (type == typeof(string))
		{
			string text = (string)value;
			if (TryParseInput(text, propertyHandle.ValueType, out value))
			{
				propertyHandle.SetValue(value);
				return;
			}
			throw new CommandExecuteException($"cannot parse \"{text}\" to type \"{propertyHandle.ValueType}\" near \"{DebugInfo}\"");
		}
		throw new CommandExecuteException($"cannot assign \"{value} ({value.GetType()})\" to {propertyHandle.Name} ({propertyHandle.ValueType}) near \"{DebugInfo}\"");
	}

	private bool TryParseInput(string input, Type type, out object data)
	{
		if (parsersCustom.TryParseSafe(type, input, out data))
		{
			return true;
		}
		if (parsersDefault.TryParse(type, input, out data))
		{
			return true;
		}
		if (type.IsEnum && Enum.TryParse(type, input, ignoreCase: true, out var result))
		{
			data = result;
			return true;
		}
		return false;
	}

	private Type TryGetTypeByAlias(string alias)
	{
		if (parsersCustom.QueryType(alias, out var type))
		{
			return type;
		}
		if (!parsersDefault.QueryType(alias, out type))
		{
			return null;
		}
		return type;
	}

	private bool TryLoadVariable(string name)
	{
		if (properties.TryGetValue(name, out var value))
		{
			_stack.Push(value);
			return true;
		}
		if (localVariables.TryGetValue(name, out var value2))
		{
			_stack.Push(value2);
			return true;
		}
		return false;
	}

	private object UnwrapStackObjectNoRequire(StackObject container)
	{
		if (container is IValueGetter valueGetter)
		{
			return valueGetter.GetValue();
		}
		if (container is StackCallable stackCallable)
		{
			return stackCallable.Instance;
		}
		if (container is StackSourceInput stackSourceInput)
		{
			return stackSourceInput.inputStr;
		}
		return null;
	}

	public object ExecuteRoot(SyntaxTree node)
	{
		Execute(node);
		if (_stack.Count == 0)
		{
			return null;
		}
		StackObject topValue = TopValue;
		_stack.Clear();
		if (topValue is IValueGetter valueGetter)
		{
			return valueGetter.GetValue();
		}
		if (topValue is StackCallable stackCallable)
		{
			return stackCallable.Instance;
		}
		if (topValue is StackSourceInput stackSourceInput)
		{
			return stackSourceInput.inputStr;
		}
		return null;
	}

	private void Execute(SyntaxTree node, ExecuteStatus status = default(ExecuteStatus))
	{
		switch (node.opcode)
		{
		case SyntaxTreeCode.OP_SET_FIELD:
			ExecuteSetField(node, status);
			break;
		case SyntaxTreeCode.OP_SET_ELEMENT:
			ExecuteSetElement(node, status);
			break;
		case SyntaxTreeCode.OP_ASSIGN:
			ExecuteAssign(node, status);
			break;
		case SyntaxTreeCode.OP_LOADVAR:
			ExecuteLoadVariable(node, status);
			break;
		case SyntaxTreeCode.OP_INDEX:
			ExecuteIndex(node, status);
			break;
		case SyntaxTreeCode.OP_CALL:
			ExecuteCall(node, status);
			break;
		case SyntaxTreeCode.OP_DOT:
			ExecuteDot(node, status);
			break;
		case SyntaxTreeCode.OP_CVT:
			ExecuteConvert(node, status);
			break;
		case SyntaxTreeCode.FACTOR_FLOAT:
			ExecuteFactorFloat(node, status);
			break;
		case SyntaxTreeCode.FACTOR_INT:
			ExecuteFactorInt(node, status);
			break;
		case SyntaxTreeCode.FACTOR_TRUE:
			ExecuteFactorTrue(node, status);
			break;
		case SyntaxTreeCode.FACTOR_FALSE:
			ExecuteFactorFalse(node, status);
			break;
		case SyntaxTreeCode.FACTOR_NULL:
			ExecuteFactorNull(node, status);
			break;
		case SyntaxTreeCode.FACTOR_STRING:
			ExecuteFactorString(node, status);
			break;
		case SyntaxTreeCode.FACTOR_INPUT:
			ExecuteFactorInput(node, status);
			break;
		case SyntaxTreeCode.FACTOR_ID:
		case SyntaxTreeCode.FACTOR_PARAMS:
			break;
		}
	}

	private void ExecuteFactorFloat(SyntaxTree node, ExecuteStatus status)
	{
		switch (status.target)
		{
		case CallTarget.RequireValue:
			if (status.requiredType != null)
			{
				if (status.requiredType.IsAssignableFrom(typeof(float)))
				{
					_stack.Push(new StackFloat(float.Parse(node.data)));
					break;
				}
				if (!TryParseInput(node.data, status.requiredType, out var data))
				{
					throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
				}
				_stack.Push(StackValue.Wrap(data));
			}
			else
			{
				_stack.Push(new StackFloat(float.Parse(node.data)));
			}
			break;
		case CallTarget.RequireCallable:
		{
			if (callables.TryGetValue(node.data, out var value))
			{
				_stack.Push(value);
				break;
			}
			throw new CommandExecuteException("cannot find callable named \"" + node.data + "\" near \"" + node.DebugInfo + "\"");
		}
		default:
			_stack.Push(new StackFloat(float.Parse(node.data)));
			break;
		}
	}

	private void ExecuteFactorInt(SyntaxTree node, ExecuteStatus status)
	{
		switch (status.target)
		{
		case CallTarget.RequireValue:
			if (status.requiredType != null)
			{
				if (status.requiredType.IsAssignableFrom(typeof(int)))
				{
					_stack.Push(new StackInt(int.Parse(node.data)));
					break;
				}
				if (!TryParseInput(node.data, status.requiredType, out var data))
				{
					throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
				}
				_stack.Push(StackValue.Wrap(data));
			}
			else
			{
				_stack.Push(new StackInt(int.Parse(node.data)));
			}
			break;
		case CallTarget.RequireCallable:
		{
			if (callables.TryGetValue(node.data, out var value))
			{
				_stack.Push(value);
				break;
			}
			throw new CommandExecuteException("cannot find callable named \"" + node.data + "\" near \"" + node.DebugInfo + "\"");
		}
		default:
			_stack.Push(new StackInt(int.Parse(node.data)));
			break;
		}
	}

	private void ExecuteFactorTrue(SyntaxTree node, ExecuteStatus status)
	{
		switch (status.target)
		{
		case CallTarget.RequireValue:
			if (status.requiredType != null)
			{
				if (status.requiredType.IsAssignableFrom(typeof(bool)))
				{
					_stack.Push(StackBool.True);
					break;
				}
				if (!TryParseInput(node.data, status.requiredType, out var data))
				{
					throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
				}
				_stack.Push(StackValue.Wrap(data));
			}
			else
			{
				_stack.Push(StackBool.True);
			}
			break;
		case CallTarget.RequireCallable:
		{
			if (callables.TryGetValue(node.data, out var value))
			{
				_stack.Push(value);
				break;
			}
			throw new CommandExecuteException("cannot find callable named \"" + node.data + "\" near \"" + node.DebugInfo + "\"");
		}
		default:
			_stack.Push(StackBool.True);
			break;
		}
	}

	private void ExecuteFactorFalse(SyntaxTree node, ExecuteStatus status)
	{
		switch (status.target)
		{
		case CallTarget.RequireValue:
			if (status.requiredType != null)
			{
				if (status.requiredType.IsAssignableFrom(typeof(bool)))
				{
					_stack.Push(StackBool.False);
					break;
				}
				if (!TryParseInput(node.data, status.requiredType, out var data))
				{
					throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
				}
				_stack.Push(StackValue.Wrap(data));
			}
			else
			{
				_stack.Push(StackBool.False);
			}
			break;
		case CallTarget.RequireCallable:
		{
			if (callables.TryGetValue(node.data, out var value))
			{
				_stack.Push(value);
				break;
			}
			throw new CommandExecuteException("cannot find callable named \"" + node.data + "\" near \"" + node.DebugInfo + "\"");
		}
		default:
			_stack.Push(StackBool.False);
			break;
		}
	}

	private void ExecuteFactorNull(SyntaxTree node, ExecuteStatus status)
	{
		switch (status.target)
		{
		case CallTarget.RequireValue:
			if (status.requiredType != null)
			{
				if (status.requiredType.IsNullable())
				{
					_stack.Push(StackNull.Default);
					break;
				}
				if (!TryParseInput(node.data, status.requiredType, out var data))
				{
					throw new CommandExecuteException($"cannot convert \"{node.data}\" to type \"{status.requiredType}\" near \"{node.DebugInfo}\"");
				}
				_stack.Push(StackValue.Wrap(data));
			}
			else
			{
				_stack.Push(StackNull.Default);
			}
			break;
		case CallTarget.RequireCallable:
		{
			if (callables.TryGetValue(node.data, out var value))
			{
				_stack.Push(value);
				break;
			}
			throw new CommandExecuteException("cannot find callable named \"" + node.data + "\" near \"" + node.DebugInfo + "\"");
		}
		default:
			_stack.Push(StackNull.Default);
			break;
		}
	}

	private bool HandleString(string input, ExecuteStatus status, string DebugInfo)
	{
		if (status.target == CallTarget.RequireValue)
		{
			if (status.requiredType != null)
			{
				if (status.requiredType.IsAssignableFrom(typeof(string)))
				{
					_stack.Push(new StackString(input));
					return true;
				}
				if (TryParseInput(input, status.requiredType, out var data))
				{
					_stack.Push(StackValue.Wrap(data));
					return true;
				}
				throw new CommandExecuteException($"cannot parse \"{input}\" to type \"{status.requiredType}\" near \"{DebugInfo}\"");
			}
			if (callables.TryGetValue(input, out var value) && value.ParameterCount == 0)
			{
				object value2 = value.Invoke(Array.Empty<object>());
				_stack.Push(StackValue.Wrap(value2));
				return true;
			}
			return false;
		}
		if (status.target == CallTarget.RequireCallable && callables.TryGetValue(input, out var value3))
		{
			_stack.Push(value3);
			return true;
		}
		return false;
	}

	private void ExecuteFactorInput(SyntaxTree node, ExecuteStatus status)
	{
		if (!HandleString(node.data, status, node.DebugInfo))
		{
			_stack.Push(new StackSourceInput(node.data));
		}
	}

	private void ExecuteFactorString(SyntaxTree node, ExecuteStatus status)
	{
		if (!HandleString(node.data, status, node.DebugInfo))
		{
			_stack.Push(new StackString(node.data));
		}
	}

	private void ExecuteSetField(SyntaxTree node, ExecuteStatus status)
	{
		Execute(node.children[0], new ExecuteStatus
		{
			target = CallTarget.RequireValue
		});
		StackObject topValue = TopValue;
		object obj = UnwrapStackObjectNoRequire(topValue) ?? throw new CommandExecuteException("cannot write value to \"null." + node.children[1].data + "\" near \"" + node.DebugInfo + "\"");
		Type type = obj.GetType();
		string data = node.children[1].data;
		MemberInfo memberInfo = type.GetDefaultMember(data) ?? throw new CommandExecuteException("\"" + node.children[0].data + "\" has no member named \"" + data + "\" near \"" + node.DebugInfo + "\"");
		Type type2;
		StackProperty stackProperty;
		switch (memberInfo.MemberType)
		{
		case MemberTypes.Field:
		{
			FieldInfo obj3 = (FieldInfo)memberInfo;
			type2 = obj3.FieldType;
			stackProperty = obj3.CreateStackProperty(obj, data) ?? throw new CommandExecuteException($"cannot write value to \"{data}\" of \"{type}\" near \"{node.DebugInfo}\"");
			break;
		}
		case MemberTypes.Property:
		{
			PropertyInfo obj2 = (PropertyInfo)memberInfo;
			type2 = obj2.PropertyType;
			stackProperty = obj2.CreateStackProperty(obj, data) ?? throw new CommandExecuteException($"cannot write value to \"{data}\" of \"{type}\" near \"{node.DebugInfo}\"");
			break;
		}
		default:
			throw new CommandExecuteException("cannot write value to \"" + node.children[0].data + "." + data + "\" near \"" + node.DebugInfo + "\"");
		}
		Execute(node.children[2], new ExecuteStatus
		{
			target = CallTarget.RequireValue,
			requiredType = type2
		});
		StackObject topValue2 = TopValue;
		if (topValue2 is IValueGetter valueGetter)
		{
			object value = valueGetter.GetValue();
			SetProperty(stackProperty, value, node.DebugInfo);
			return;
		}
		if (topValue2 is StackSourceInput stackSourceInput)
		{
			if (TryParseInput(stackSourceInput.inputStr, type2, out var data2))
			{
				stackProperty.SetValue(data2);
				return;
			}
			throw new CommandExecuteException($"cannot parse \"{stackSourceInput.inputStr}\" to type {type2} near \"{node.DebugInfo}\"");
		}
		throw new CommandExecuteException($"cannot assign <{topValue2.stackType}> to {topValue2.stackType} near \"{node.DebugInfo}\"");
	}

	private void ExecuteLoadVariable(SyntaxTree node, ExecuteStatus status)
	{
		string data = node.data;
		if (!TryLoadVariable(data))
		{
			StackPropertyCustom stackPropertyCustom = new StackPropertyCustom(data);
			localVariables.Add(data, stackPropertyCustom);
			_stack.Push(stackPropertyCustom);
		}
	}

	private void ExecuteDot(SyntaxTree node, ExecuteStatus status)
	{
		Execute(node.children[0], new ExecuteStatus
		{
			target = CallTarget.RequireValue
		});
		StackObject topValue = TopValue;
		object obj;
		if (topValue is IValueGetter valueGetter)
		{
			obj = valueGetter.GetValue() ?? throw new CommandExecuteException("cannot get member from null near \"" + node.DebugInfo + "\"");
		}
		else if (topValue is StackCallable stackCallable)
		{
			obj = stackCallable.Instance;
		}
		else
		{
			if (!(topValue is StackSourceInput stackSourceInput))
			{
				throw new CommandExecuteException($"cannot get member from <{topValue.stackType}> near \"{node.DebugInfo}\"");
			}
			obj = stackSourceInput.inputStr;
		}
		Type type = obj.GetType();
		string data = node.children[1].data;
		MemberInfo memberInfo = type.GetDefaultMember(data) ?? throw new CommandExecuteException($"\"{type}\" has no member named \"{data}\" near \"{node.DebugInfo}\"");
		StackObject item = memberInfo.MemberType switch
		{
			MemberTypes.Field => ((FieldInfo)memberInfo).CreateStackProperty(obj, data) ?? throw new CommandExecuteException($"invalid member dot \"{data}\" from {topValue.GetType()} around \"{node.DebugInfo}\""), 
			MemberTypes.Property => ((PropertyInfo)memberInfo).CreateStackProperty(obj, data) ?? throw new CommandExecuteException($"invalid member dot \"{data}\" from {topValue.GetType()} around \"{node.DebugInfo}\""), 
			MemberTypes.Method => ((MethodInfo)memberInfo).CreateStackCallable(obj) ?? throw new CommandExecuteException($"invalid member dot \"{data}\" from {topValue.GetType()} around \"{node.DebugInfo}\""), 
			_ => throw new CommandExecuteException($"cannot get member \"{data}\" from {topValue.GetType()} around \"{node.DebugInfo}\""), 
		};
		_stack.Push(item);
	}

	private void ExecuteCall(SyntaxTree node, ExecuteStatus status = default(ExecuteStatus))
	{
		Execute(node.children[0], new ExecuteStatus
		{
			target = CallTarget.RequireCallable
		});
		StackObject topValue = TopValue;
		if (topValue is StackCallable stackCallable)
		{
			SyntaxTree syntaxTree = node.children[1];
			object[] array = new object[stackCallable.ParameterCount];
			for (int i = 0; i < stackCallable.ParameterCount; i++)
			{
				ParameterInfo parameter = stackCallable.GetParameter(i);
				Type parameterType = parameter.ParameterType;
				if (i < syntaxTree.children.Length)
				{
					Execute(syntaxTree.children[i], new ExecuteStatus
					{
						target = CallTarget.RequireValue,
						requiredType = parameterType
					});
					topValue = TopValue;
					if (topValue is IValueGetter valueGetter)
					{
						object value = valueGetter.GetValue();
						if (value == null)
						{
							if (parameterType.IsNullable())
							{
								array[i] = null;
								continue;
							}
							throw new CommandExecuteException($"invalid parameter value \"null\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
						}
						Type type = value.GetType();
						if (parameterType.IsAssignableFrom(type))
						{
							array[i] = value;
							continue;
						}
						string input = value.ToString() ?? throw new CommandExecuteException($"invalid parameter type \"{value.GetType()}\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
						if (!TryParseInput(input, parameterType, out var data))
						{
							throw new CommandExecuteException($"invalid parameter type \"{value.GetType()}\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
						}
						array[i] = data;
					}
					else if (topValue is StackCallable { Instance: var instance })
					{
						if (!parameterType.IsAssignableFrom(instance.GetType()))
						{
							throw new CommandExecuteException($"invalid parameter type \"{instance.GetType()}\" for \"{parameter.Name}\" ({parameterType}) near \"{node.DebugInfo}\"");
						}
						array[i] = instance;
					}
					else if (topValue is StackSourceInput { inputStr: var inputStr })
					{
						if (parameterType == typeof(string))
						{
							array[i] = inputStr;
							continue;
						}
						if (!TryParseInput(inputStr, parameterType, out var data2))
						{
							throw new CommandExecuteException($"cannot parse \"{inputStr}\" to type \"{parameterType}\" near \"{node.DebugInfo}\"");
						}
						array[i] = data2;
					}
					else if (parameterType.IsNullable())
					{
						array[i] = null;
					}
					else
					{
						array[i] = 0;
					}
				}
				else
				{
					if (!stackCallable.TryGetParameterDefaultValue(i, out var defaultValue))
					{
						throw new CommandExecuteException("parameter \"" + parameter.Name + "\" of method \"" + stackCallable.Name + "\" is not provided");
					}
					array[i] = defaultValue;
				}
			}
			if (stackCallable.HasReturnValue)
			{
				_stack.Push(StackValue.Wrap(stackCallable.Invoke(array)));
				return;
			}
			stackCallable.Invoke(array);
			if (TreatVoidAsNull)
			{
				_stack.Push(StackNull.Default);
			}
			return;
		}
		if (topValue is StackSourceInput { inputStr: var inputStr2 })
		{
			if (status.requiredType == null)
			{
				throw new CommandExecuteException("don't know how to handle input \"" + inputStr2 + "\" near \"" + node.DebugInfo + "\"");
			}
			if (status.requiredType == typeof(string))
			{
				_stack.Push(new StackString(inputStr2));
				return;
			}
			if (TryParseInput(inputStr2, status.requiredType, out var data3))
			{
				_stack.Push(StackValue.Wrap(data3));
				return;
			}
			throw new CommandExecuteException($"cannot parse \"{inputStr2}\" to type {status.requiredType} near \"{node.DebugInfo}\"");
		}
		throw new CommandExecuteException($"<{topValue.stackType}> is not callable near \"{node.DebugInfo}\"");
	}

	private void ExecuteAssign(SyntaxTree node, ExecuteStatus status)
	{
		Execute(node.children[0]);
		StackObject topValue = TopValue;
		if (!(topValue is IValueSetter valueSetter))
		{
			if (!IgnoreInvalidSetBehaviour)
			{
				throw new CommandExecuteException($"cannot assign value to <{topValue.stackType}> near \"{node.DebugInfo}\"");
			}
			Execute(node.children[1]);
			return;
		}
		if (valueSetter is StackPropertyCustom)
		{
			Execute(node.children[1], new ExecuteStatus
			{
				target = CallTarget.RequireValue
			});
		}
		else
		{
			Execute(node.children[1], new ExecuteStatus
			{
				target = CallTarget.RequireValue,
				requiredType = valueSetter.ValueType
			});
		}
		topValue = TopValue;
		if (topValue is IValueGetter valueGetter)
		{
			object value = valueGetter.GetValue();
			SetProperty(valueSetter, value, node.DebugInfo);
			return;
		}
		if (topValue is StackSourceInput stackSourceInput)
		{
			if (valueSetter is StackPropertyCustom stackPropertyCustom)
			{
				stackPropertyCustom.SetValue(stackSourceInput.inputStr);
				return;
			}
			if (TryParseInput(stackSourceInput.inputStr, valueSetter.ValueType, out var value))
			{
				valueSetter.SetValue(value);
				return;
			}
			throw new CommandExecuteException($"cannot parse \"{stackSourceInput.inputStr}\" to type \"{valueSetter.ValueType}\" near \"{node.DebugInfo}\"");
		}
		throw new CommandExecuteException($"cannot assign <{topValue.stackType}> to \"{topValue.stackType}\" near \"{node.DebugInfo}\"");
	}

	private void ExecuteIndex(SyntaxTree node, ExecuteStatus status)
	{
		Execute(node.children[0]);
		StackObject topValue = TopValue;
		object obj = ((topValue as IValueGetter) ?? throw new CommandExecuteException($"cannot get element from <{topValue.stackType}> near \"{node.DebugInfo}\"")).GetValue() ?? throw new CommandExecuteException("cannot get element from \"null\" near \"" + node.DebugInfo + "\"");
		Type type = obj.GetType();
		Execute(node.children[1]);
		topValue = TopValue;
		if (topValue is StackInt stackInt)
		{
			object element = VirtualMachineUtils.GetElement(obj, stackInt.intValue);
			_stack.Push(StackValue.Wrap(element));
			return;
		}
		if (topValue is StackBool stackBool)
		{
			object element2 = VirtualMachineUtils.GetElement(obj, stackBool.boolValue ? 1 : 0);
			_stack.Push(StackValue.Wrap(element2));
			return;
		}
		if (topValue is IValueGetter valueGetter)
		{
			object value = valueGetter.GetValue();
			object element3 = VirtualMachineUtils.GetElement(obj, value);
			_stack.Push(StackValue.Wrap(element3));
			return;
		}
		throw new CommandExecuteException($"<{topValue.stackType}> cannot be index of \"{type}\" near \"{node.DebugInfo}\"");
	}

	private void ExecuteSetElement(SyntaxTree node, ExecuteStatus status)
	{
		Execute(node.children[1]);
		StackObject topValue = TopValue;
		object obj = ((topValue as IValueGetter) ?? throw new CommandExecuteException($"<{topValue.stackType}> cannot be index near \"{node.DebugInfo}\"")).GetValue() ?? throw new CommandExecuteException("null cannot be index \"" + node.DebugInfo + "\"");
		bool flag = topValue is StackInt || topValue is StackBool;
		Execute(node.children[0]);
		topValue = TopValue;
		object obj2 = ((topValue as IValueGetter) ?? throw new CommandExecuteException($"cannot set element of <{topValue.stackType}> near \"{node.DebugInfo}\"")).GetValue() ?? throw new CommandExecuteException("cannot set element of null near \"" + node.DebugInfo + "\"");
		Type type = null;
		if (flag)
		{
			type = VirtualMachineUtils.GetElementTypeOfArray(obj2);
		}
		if (type == null)
		{
			flag = false;
			type = VirtualMachineUtils.GetElementTypeOfDict(obj2, obj.GetType()) ?? throw new CommandExecuteException($"cannot set element of <{topValue.stackType}> near \"{node.DebugInfo}\"");
		}
		Execute(node.children[2], new ExecuteStatus
		{
			requiredType = type
		});
		topValue = TopValue;
		object value = ((topValue as IValueGetter) ?? throw new CommandExecuteException($"cannot set <{topValue.stackType}> as element of {obj2.GetType()} near \"{node.DebugInfo}\"")).GetValue();
		if (flag)
		{
			if (topValue is StackInt stackInt)
			{
				VirtualMachineUtils.SetElement(obj2, stackInt.intValue, value);
				return;
			}
			if (!(topValue is StackBool stackBool))
			{
				throw new CommandExecuteException($"cannot convert \"{obj}\" to int type near \"{node.DebugInfo}\"");
			}
			VirtualMachineUtils.SetElement(obj2, stackBool.boolValue ? 1 : 0, value);
		}
		else
		{
			VirtualMachineUtils.SetElement(obj2, obj, value);
		}
	}

	private void ExecuteConvert(SyntaxTree node, ExecuteStatus status)
	{
		Execute(node.children[0]);
		StackObject topValue = TopValue;
		string data = node.children[1].data;
		Type type = TryGetTypeByAlias(data) ?? status.requiredType;
		if (type == null)
		{
			_stack.Push(topValue);
			return;
		}
		if (topValue is IValueGetter valueGetter)
		{
			object value = valueGetter.GetValue();
			if (value == null)
			{
				if (type.IsNullable())
				{
					_stack.Push(StackNull.Default);
					return;
				}
				throw new CommandExecuteException($"cannot convert \"null\" to \"{status.requiredType}\" near \"{node.DebugInfo}\"");
			}
			if (type.IsAssignableFrom(value.GetType()))
			{
				_stack.Push(topValue);
				return;
			}
			string text = value.ToString();
			if (text == null)
			{
				_stack.Push(topValue);
				return;
			}
			if (type == typeof(string))
			{
				_stack.Push(new StackString(text));
				return;
			}
			if (TryParseInput(text, type, out var data2))
			{
				_stack.Push(StackValue.Wrap(data2));
				return;
			}
		}
		if (topValue is StackSourceInput stackSourceInput)
		{
			if (type == typeof(string))
			{
				_stack.Push(new StackString(stackSourceInput.inputStr));
				return;
			}
			if (TryParseInput(stackSourceInput.inputStr, type, out var data3))
			{
				_stack.Push(StackValue.Wrap(data3));
				return;
			}
		}
		_stack.Push(topValue);
	}
}
