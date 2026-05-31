using System;

namespace RedSaw.CommandLineInterface;

public readonly struct LexerResult
{
	public readonly string source;

	public readonly Token[] tokens;

	public readonly string[] sourceBuffer;

	public Token this[int index] => tokens[index];

	public Token LastToken => tokens[^1];

	public LexerResult(string source, Token[] tokens, string[] sourceBuffer)
	{
		this.source = source;
		this.tokens = tokens;
		this.sourceBuffer = sourceBuffer;
	}

	public void Debug(Action<string> log)
	{
		Token[] array = tokens;
		for (int i = 0; i < array.Length; i++)
		{
			Token token = array[i];
			string arg = ((token.sourceIdx >= 0) ? sourceBuffer[token.sourceIdx] : string.Empty);
			log($"({token.type}) {arg}");
		}
	}

	public string NearInfo(Token token, int radius = 7)
	{
		int num = Math.Max(0, token.startIdx - radius);
		int num2 = Math.Min(token.endIdx + radius, source.Length - 1);
		return source[num..num2];
	}

	public string NearInfo(int LB, int RB)
	{
		int num = Math.Max(0, LB);
		int num2 = Math.Min(RB, source.Length - 1);
		return source[num..num2];
	}
}
