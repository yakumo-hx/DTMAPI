using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RedSaw.CommandLineInterface;

public class CommandSystem
{
	private readonly Lexer lexer;

	private readonly SyntaxAnalyzer syntaxAnalyzer;

	private readonly float scoreThresholdCommand;

	private readonly float scoreThresholdVariable;

	private readonly float scoreThresholdType;

	private readonly QueryCache<Suggestion> QC_command;

	private readonly QueryCache<Suggestion> QC_variable;

	private readonly QueryCache<Suggestion> QC_type;

	private readonly VirtualMachine vm;

	private readonly CharAutomaton charAutomaton;

	private string lastQueryStr = string.Empty;

	public CommandSystem(float scoreThresholdCommand = 0.3f, float scoreThresholdVariable = 0.1f, float scoreThresholdType = 0.1f, int typeReflectionQueryCache = 20, int commandQueryCacheCapacity = 20, int variableQueryCacheCapacity = 20, bool IgnoreInvalidSetBehaviour = true, bool ReceiveValueFromNonStringType = true, bool TreatVoidAsNull = true)
	{
		lexer = new Lexer();
		syntaxAnalyzer = new SyntaxAnalyzer();
		vm = new VirtualMachine(IgnoreInvalidSetBehaviour, ReceiveValueFromNonStringType, TreatVoidAsNull);
		charAutomaton = new CharAutomaton(vm.GetPropertyType, vm.GetCallableType);
		this.scoreThresholdCommand = Math.Clamp(scoreThresholdCommand, 0f, 1f);
		this.scoreThresholdVariable = Math.Clamp(scoreThresholdVariable, 0f, 1f);
		this.scoreThresholdType = Math.Clamp(scoreThresholdType, 0f, 1f);
		QC_command = new QueryCache<Suggestion>(commandQueryCacheCapacity);
		QC_variable = new QueryCache<Suggestion>(variableQueryCacheCapacity);
		QC_type = new QueryCache<Suggestion>(typeReflectionQueryCache);
		foreach (Command item in CommandCreator.CollectCommands<CommandAttribute>())
		{
			vm.RegisterCallable(item);
		}
		foreach (StackProperty item2 in CommandCreator.CollectProperties<CommandPropertyAttribute>())
		{
			vm.RegisterProperty(item2);
		}
		foreach (var item3 in CommandCreator.CollectValueParsers<CommandValueParserAttribute>())
		{
			vm.RegisterValueParser(item3.Item2, item3.Item1, item3.Item3);
		}
	}

	public void RegisterCommand(Command command)
	{
		vm.RegisterCallable(command);
	}

	public void RegisterProperty(StackProperty property)
	{
		vm.RegisterProperty(property);
	}

	public void RegisterValueParser(Type type, ValueParser parser, string alias = null)
	{
		vm.RegisterValueParser(type, parser, alias);
	}

	public void RegisterValueParser<TType>(ValueParser parser, string alias = null)
	{
		vm.RegisterValueParser(typeof(TType), parser, alias);
	}

	public object GetLocalVariable(string name)
	{
		return vm.GetLocalVariable(name);
	}

	public void SetLocalVariable(string name, object value)
	{
		vm.SetLocalVariable(name, value);
	}

	public Suggestion[] GetCurrentSuggestions(string currentText, int count, Func<string, string, float> scoreFunc)
	{
		SuggestionQuery suggestionQuery = charAutomaton.Input(currentText);
		lastQueryStr = suggestionQuery.queryStr;
		return suggestionQuery.suggestionType switch
		{
			SuggestionType.Variable => QueryVariable(suggestionQuery.queryStr, count, scoreFunc), 
			SuggestionType.Command => QueryCommands(suggestionQuery.queryStr, count, scoreFunc), 
			SuggestionType.Member => QueryType(suggestionQuery.queryStr, suggestionQuery.queryType, count, scoreFunc), 
			_ => Array.Empty<Suggestion>(), 
		};
	}

	public string TakeSuggestion(string currentInput, string primary)
	{
		if (lastQueryStr == string.Empty)
		{
			return currentInput + primary;
		}
		return currentInput[..^lastQueryStr.Length] + primary;
	}

	public Delegate GetFunction(string name)
	{
		return vm.GetDelegate(name);
	}

	public IEnumerable<(string, Delegate)> GetAllFunctions()
	{
		return vm.GetAllDelegate();
	}

	public Exception Execute(string commandInput, out object executeResult)
	{
		try
		{
			LexerResult lexerResult = lexer.Parse(commandInput);
			SyntaxTree node = syntaxAnalyzer.Analyze(lexerResult);
			executeResult = vm.ExecuteRoot(node);
			return null;
		}
		catch (CommandSystemException result)
		{
			executeResult = null;
			return result;
		}
		catch (Exception ex)
		{
			executeResult = null;
			return ex.InnerException ?? ex;
		}
	}

	public Suggestion[] QueryCommands(string query, int count, Func<string, string, float> scoreFunc, string tag = null)
	{
		if (query == null)
		{
			query = string.Empty;
		}
		string query2 = query + ":" + tag;
		if (QC_command.GetCache(query2, out var result))
		{
			return result;
		}
		result = (from s in vm.AllCallables
			where s is Command command2 && command2.CompareTag(tag)
			select new
			{
				value = s,
				score = scoreFunc(query, s.Name)
			} into s
			orderby s.score descending
			select s).Take(Math.Max(1, count)).Select(s =>
		{
			Command command = (Command)s.value;
			return new Suggestion(command.Name, command.description);
		}).ToArray();
		QC_command.Cache(query2, result);
		return result;
	}

	public Suggestion[] QueryVariable(string query, int count, Func<string, string, float> scoreFunc, string tag = null)
	{
		if (query == null)
		{
			query = string.Empty;
		}
		string query2 = query + ":" + tag;
		if (QC_variable.GetCache(query2, out var result))
		{
			return result;
		}
		result = (from s in (from s in vm.AllProperties
				where s.CompareTag(tag)
				select new
				{
					value = s,
					score = scoreFunc(query, s.Name)
				} into s
				orderby s.score descending
				select s).Take(Math.Max(1, count))
			select new Suggestion(s.value.Name, s.value.description)).ToArray();
		QC_variable.Cache(query2, result);
		return result;
	}

	public Suggestion[] QueryType(string query, Type type, int count, Func<string, string, float> scoreFunc)
	{
		if (query == null)
		{
			query = string.Empty;
		}
		string query2 = type.Name + "." + query;
		if (QC_type.GetCache(query2, out var result))
		{
			return result;
		}
		result = (from s in (from s in type.GetMembers()
				select new
				{
					value = s,
					score = scoreFunc(query, s.Name)
				} into s
				orderby s.score descending
				select s).Take(Math.Max(1, count))
			select new Suggestion(s.value.Name, s.value.GetMemberTypeName())).ToArray();
		QC_type.Cache(query2, result);
		return result;
	}
}
