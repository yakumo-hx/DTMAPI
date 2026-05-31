using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace XLua.TemplateEngine;

public class Parser
{
	public static string RegexString { get; private set; }

	static Parser()
	{
		RegexString = GetRegexString();
	}

	private static string EscapeString(string input)
	{
		return input.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"")
			.Replace("\n", "\\n")
			.Replace("\t", "\\t")
			.Replace("\r", "\\r")
			.Replace("\b", "\\b")
			.Replace("\f", "\\f")
			.Replace("\a", "\\a")
			.Replace("\v", "\\v")
			.Replace("\0", "\\0");
	}

	private static string GetRegexString()
	{
		string text = "(?<error>((?!<%).)*%>)";
		string text2 = "(?<text>((?!<%).)+)";
		string text3 = "(?<nocode><%=?%>)";
		string text4 = "<%(?<code>[^=]((?!<%|%>).)*)%>";
		string text5 = "<%=(?<eval>((?!<%|%>).)*)%>";
		string text6 = "(?<error><%.*)";
		string text7 = "(?<error>^$)";
		return "(" + text + "|" + text2 + "|" + text3 + "|" + text4 + "|" + text5 + "|" + text6 + "|" + text7 + ")*";
	}

	public static List<Chunk> Parse(string snippet)
	{
		Match match = new Regex(RegexString, RegexOptions.ExplicitCapture | RegexOptions.Singleline).Match(snippet);
		if (match.Groups["error"].Length > 0)
		{
			throw new TemplateFormatException("Messed up brackets");
		}
		List<Chunk> list = (from p in (from Capture p in match.Groups["code"].Captures
				select new
				{
					Type = TokenType.Code,
					Value = p.Value,
					Index = p.Index
				}).Concat(from Capture p in match.Groups["text"].Captures
				select new
				{
					Type = TokenType.Text,
					Value = EscapeString(p.Value),
					Index = p.Index
				}).Concat(from Capture p in match.Groups["eval"].Captures
				select new
				{
					Type = TokenType.Eval,
					Value = p.Value,
					Index = p.Index
				})
			orderby p.Index
			select p into m
			select new Chunk(m.Type, m.Value)).ToList();
		if (list.Count == 0)
		{
			throw new TemplateFormatException("Empty template");
		}
		return list;
	}
}
