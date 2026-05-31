namespace RedSaw.CommandLineInterface;

public readonly struct Token
{
	public readonly TokenType type;

	public readonly int sourceIdx;

	public readonly int startIdx;

	public readonly int endIdx;

	public Token(TokenType type, int sourceId, int startIdx, int endIdx)
	{
		this.type = type;
		sourceIdx = sourceId;
		this.startIdx = startIdx;
		this.endIdx = endIdx;
	}
}
