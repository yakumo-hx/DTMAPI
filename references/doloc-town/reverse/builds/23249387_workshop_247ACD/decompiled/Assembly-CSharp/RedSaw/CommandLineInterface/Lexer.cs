using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

public class Lexer
{
	public const char EOL = '\n';

	public const char EOF = '\0';

	public const char TAB = '\t';

	public const char CR = '\r';

	public const char WHITE_SPACE = ' ';

	public const char VAR = '@';

	public const char DOT = '.';

	public const char COLON = ':';

	public const char COMMA = ',';

	public const char ASSIGN = '=';

	public const char L_PAREN = '(';

	public const char R_PAREN = ')';

	public const char L_BRACKET = '[';

	public const char R_BRACKET = ']';

	public const char SINGLE_QUOTE = '\'';

	public const char DOUBLE_QUOTE = '"';

	public const char UNDERLINE = '_';

	public const char NEGATIVE = '-';

	public const char E = 'e';

	public const string TRUE = "true";

	public const string FALSE = "false";

	public const string NULL = "null";

	public static char[] WHITE_SPACE_CHARS = new char[4] { '\n', '\0', '\t', '\r' };

	public static char[] SYMBOLS = new char[11]
	{
		'@', '.', ':', ',', '=', '(', ')', '[', ']', '\'',
		'"'
	};

	public static char[] ID_TERMINATORS = new char[12]
	{
		' ', '\0', '.', ':', ',', '=', '(', ')', '[', ']',
		'\'', '"'
	};

	private int index;

	private string src;

	private readonly List<string> tokenValues = new List<string>();

	private readonly List<Token> tokens = new List<Token>();

	private bool HasMore => index < src.Length;

	public static bool IsId(string value)
	{
		if (value == null || value.Length == 0)
		{
			return false;
		}
		char c = value[0];
		if (c == '_' || char.IsLetter(c))
		{
			for (int i = 1; i < value.Length; i++)
			{
				char c2 = value[i];
				if (c2 != '_')
				{
					char.IsLetterOrDigit(c2);
				}
			}
			return true;
		}
		return false;
	}

	public static bool IsWhiteSpace(char c)
	{
		char[] wHITE_SPACE_CHARS = WHITE_SPACE_CHARS;
		foreach (char c2 in wHITE_SPACE_CHARS)
		{
			if (c == c2)
			{
				return true;
			}
		}
		return char.IsWhiteSpace(c);
	}

	private bool IsInputTerminator(char c)
	{
		char[] iD_TERMINATORS = ID_TERMINATORS;
		foreach (char c2 in iD_TERMINATORS)
		{
			if (c == c2)
			{
				return true;
			}
		}
		return false;
	}

	private string PreProcess(string input)
	{
		char[] wHITE_SPACE_CHARS = WHITE_SPACE_CHARS;
		foreach (char oldChar in wHITE_SPACE_CHARS)
		{
			input = input.Replace(oldChar, ' ');
		}
		return input + "\0";
	}

	public LexerResult Parse(string input)
	{
		if (input == null || input.Length == 0)
		{
			throw new CommandLexerException("Input string is empty");
		}
		string text = PreProcess(input);
		if (text.Length == 0)
		{
			throw new CommandLexerException("Input string is invalid");
		}
		return Walk(text);
	}

	private LexerResult Walk(string input)
	{
		src = input;
		index = 0;
		tokens.Clear();
		tokenValues.Clear();
		while (HasMore)
		{
			int startIdx = index;
			char c = src[index++];
			switch (c)
			{
			case '\0':
				tokens.Add(new Token(TokenType.TK_EOF, -1, startIdx, index));
				continue;
			case '@':
			{
				string item = NextInput(index);
				tokens.Add(new Token(TokenType.TK_VAR, tokenValues.Count, startIdx, index));
				tokenValues.Add(item);
				continue;
			}
			case '"':
			case '\'':
			{
				string item2 = NextString(index, c);
				tokens.Add(new Token(TokenType.TK_STRING, tokenValues.Count, startIdx, index));
				tokenValues.Add(item2);
				continue;
			}
			case ':':
				tokens.Add(new Token(TokenType.TK_COLON, -1, startIdx, index));
				continue;
			case ',':
				startIdx = index - 1;
				tokens.Add(new Token(TokenType.TK_COMMA, -1, startIdx, index));
				continue;
			case '.':
				tokens.Add(new Token(TokenType.TK_DOT, -1, startIdx, index));
				continue;
			case '=':
				tokens.Add(new Token(TokenType.TK_ASSIGN, -1, startIdx, index));
				continue;
			case '(':
				tokens.Add(new Token(TokenType.TK_L_PAREN, -1, startIdx, index));
				continue;
			case ')':
				tokens.Add(new Token(TokenType.TK_R_PAREN, -1, startIdx, index));
				continue;
			case '[':
				tokens.Add(new Token(TokenType.TK_L_BRACKET, -1, startIdx, index));
				continue;
			case ']':
				tokens.Add(new Token(TokenType.TK_R_BRACKET, -1, startIdx, index));
				continue;
			case ' ':
				continue;
			}
			if (c == '-' || char.IsDigit(c))
			{
				bool isFloat;
				string item3 = NextNumber(index, out isFloat);
				tokens.Add(new Token(isFloat ? TokenType.TK_FLOAT : TokenType.TK_INT, tokenValues.Count, startIdx, index));
				tokenValues.Add(item3);
				continue;
			}
			string text = NextInput(index - 1);
			switch (text)
			{
			case "true":
				tokens.Add(new Token(TokenType.TK_TRUE, -2, startIdx, index));
				break;
			case "false":
				tokens.Add(new Token(TokenType.TK_FALSE, -3, startIdx, index));
				break;
			case "null":
				tokens.Add(new Token(TokenType.TK_NULL, -4, startIdx, index));
				break;
			default:
				tokens.Add(new Token(TokenType.TK_INPUT, tokenValues.Count, startIdx, index));
				tokenValues.Add(text);
				break;
			}
		}
		return new LexerResult(input, tokens.ToArray(), tokenValues.ToArray());
	}

	private string NextInput(int startIdx)
	{
		while (HasMore)
		{
			char c = src[index++];
			if (IsInputTerminator(c))
			{
				index--;
				break;
			}
		}
		return src[startIdx..index];
	}

	private string NextString(int startIdx, char quoteChar)
	{
		while (HasMore)
		{
			if (src[index++] == quoteChar)
			{
				return src[startIdx..(index - 1)];
			}
		}
		string text = src[startIdx..index];
		throw new CommandLexerException("String not terminated near \".." + text + "\"");
	}

	private string NextNumber(int startIdx, out bool isFloat)
	{
		while (HasMore)
		{
			char c = src[index++];
			if (c == '.')
			{
				isFloat = true;
				return NextFloat(startIdx);
			}
			if (!char.IsDigit(c))
			{
				index--;
				break;
			}
		}
		isFloat = false;
		return src[(startIdx - 1)..index];
	}

	private string NextFloat(int startIdx)
	{
		while (HasMore)
		{
			char c = src[index++];
			if (!char.IsDigit(c))
			{
				if (c == 'e')
				{
					return NextExponent(startIdx);
				}
				index--;
				break;
			}
		}
		return src[(startIdx - 1)..index];
	}

	private string NextExponent(int startIdx)
	{
		while (HasMore)
		{
			if (!char.IsDigit(src[index++]))
			{
				index--;
				break;
			}
		}
		return src[(startIdx - 1)..index];
	}
}
