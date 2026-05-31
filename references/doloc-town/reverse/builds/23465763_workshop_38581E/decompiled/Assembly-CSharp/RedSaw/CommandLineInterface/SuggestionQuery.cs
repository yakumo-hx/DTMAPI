using System;

namespace RedSaw.CommandLineInterface;

public readonly struct SuggestionQuery
{
	public static readonly SuggestionQuery None = new SuggestionQuery(SuggestionType.None, string.Empty);

	public readonly SuggestionType suggestionType;

	public readonly string queryStr;

	public readonly Type queryType;

	public SuggestionQuery(SuggestionType suggestionType, string queryStr, Type queryType)
	{
		this.suggestionType = suggestionType;
		this.queryStr = queryStr;
		this.queryType = queryType;
	}

	public SuggestionQuery(SuggestionType suggestionType, string queryStr)
		: this(suggestionType, queryStr, null)
	{
	}

	public override string ToString()
	{
		return suggestionType switch
		{
			SuggestionType.Variable => "Variable -> \"" + queryStr + "\"", 
			SuggestionType.Command => "Command -> \"" + queryStr + "\"", 
			SuggestionType.Member => "Member -> \"" + queryType.Name + "." + queryStr + "\"", 
			_ => "No Suggestions", 
		};
	}
}
