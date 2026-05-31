using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

public class ValueParserManager
{
	private readonly Dictionary<Type, ValueParser> parsers = new Dictionary<Type, ValueParser>();

	private readonly Dictionary<string, Type> typeAlias = new Dictionary<string, Type>();

	public ValueParserManager()
	{
	}

	public ValueParserManager(Dictionary<Type, ValueParser> parsers, Dictionary<string, Type> typeAlias)
	{
		this.parsers = parsers;
		this.typeAlias = typeAlias;
	}

	public void RegisterParser(Type type, ValueParser parser, string typeAlias = null)
	{
		if (parsers.ContainsKey(type))
		{
			parsers[type] = parser;
		}
		else
		{
			parsers.Add(type, parser);
		}
		string key = typeAlias ?? type.Name;
		if (this.typeAlias.ContainsKey(key))
		{
			this.typeAlias[key] = type;
		}
		else
		{
			this.typeAlias.Add(key, type);
		}
	}

	public bool TryParse(Type type, string inputStr, out object data)
	{
		if (parsers.TryGetValue(type, out var value))
		{
			return value(inputStr, out data);
		}
		data = null;
		return false;
	}

	public bool TryParseSafe(Type type, string inputStr, out object data)
	{
		if (parsers.TryGetValue(type, out var value))
		{
			try
			{
				return value(inputStr, out data);
			}
			catch (Exception)
			{
				data = null;
				return false;
			}
		}
		data = null;
		return false;
	}

	public bool QueryType(string alias, out Type type)
	{
		return typeAlias.TryGetValue(alias, out type);
	}
}
