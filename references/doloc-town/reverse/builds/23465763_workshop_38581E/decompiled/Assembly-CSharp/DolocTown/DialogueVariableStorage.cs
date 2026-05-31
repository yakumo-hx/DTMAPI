using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Yarn;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class DialogueVariableStorage : IVariableStorage
{
	[JsonProperty]
	private Dictionary<string, object> variables;

	[JsonProperty]
	private Dictionary<string, Type> variableTypes;

	[JsonConstructor]
	public DialogueVariableStorage(Dictionary<string, object> variables = null, Dictionary<string, Type> variableTypes = null)
	{
		this.variables = variables ?? new Dictionary<string, object>();
		this.variableTypes = variableTypes ?? new Dictionary<string, Type>();
	}

	private void SetVariable(string name, IType type, string value)
	{
		if (object.Equals(type, BuiltinTypes.Boolean))
		{
			if (!bool.TryParse(value, out var result))
			{
				throw new InvalidCastException("Couldn't initialize default variable " + name + " with value " + value + " as Bool");
			}
			SetValue(name, result);
		}
		else if (object.Equals(type, BuiltinTypes.Number))
		{
			if (!float.TryParse(value, out var result2))
			{
				throw new InvalidCastException("Couldn't initialize default variable " + name + " with value " + value + " as Number (Float)");
			}
			SetValue(name, result2);
		}
		else
		{
			if (!object.Equals(type, BuiltinTypes.String))
			{
				throw new ArgumentOutOfRangeException("Unsupported type " + type.Name);
			}
			SetValue(name, value);
		}
	}

	private void ValidateVariableName(string variableName)
	{
		if (!variableName.StartsWith("$"))
		{
			throw new ArgumentException(variableName + " is not a valid variable name: Variable names must start with a '$'. (Did you mean to use '$" + variableName + "'?)");
		}
	}

	public void SetValue(string variableName, string stringValue)
	{
		ValidateVariableName(variableName);
		variables[variableName] = stringValue;
		variableTypes[variableName] = typeof(string);
	}

	public void SetValue(string variableName, float floatValue)
	{
		ValidateVariableName(variableName);
		variables[variableName] = floatValue;
		variableTypes[variableName] = typeof(float);
	}

	public void SetValue(string variableName, bool boolValue)
	{
		ValidateVariableName(variableName);
		variables[variableName] = boolValue;
		variableTypes[variableName] = typeof(bool);
	}

	public bool TryGetValue<T>(string variableName, out T result)
	{
		ValidateVariableName(variableName);
		if (!variables.ContainsKey(variableName))
		{
			result = default(T);
			return false;
		}
		object obj = variables[variableName];
		if (obj is T val)
		{
			result = val;
			return true;
		}
		throw new InvalidCastException($"Variable {variableName} exists, but is the wrong type (expected {typeof(T)}, got {obj.GetType()}");
	}

	public bool TryGetBoolValue(string variableName, out bool result)
	{
		return TryGetValue<bool>(variableName, out result);
	}

	public bool TryGetNumberValue(string variableName, out float result)
	{
		return TryGetValue<float>(variableName, out result);
	}

	public bool TryGetStringValue(string variableName, out string result)
	{
		return TryGetValue<string>(variableName, out result);
	}

	public void Clear()
	{
		variables.Clear();
		variableTypes.Clear();
	}

	public bool Contains(string variableName)
	{
		return variables.ContainsKey(variableName);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, object> variable in variables)
		{
			stringBuilder.AppendLine(string.Format("{0} = {1} ({2})", variable.Key, variable.Value.ToString(), variableTypes[variable.Key].ToString().Substring("System.".Length)));
		}
		return stringBuilder.ToString();
	}
}
