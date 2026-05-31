using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

public class SyntaxAnalyzer
{
	private LexerResult lexerResult;

	private int index;

	private bool __command_latch;

	private Token[] Tokens => lexerResult.tokens;

	private string[] TokenValues => lexerResult.sourceBuffer;

	public bool HasMore => index < Tokens.Length;

	public Token CurrentToken => Tokens[index];

	public Token NextToken => Tokens[index++];

	public SyntaxTree Analyze(LexerResult lexerResult)
	{
		this.lexerResult = lexerResult;
		index = 0;
		__command_latch = false;
		return NextExpr(isRoot: true);
	}

	private bool Match(TokenType tokenType)
	{
		if (HasMore && CurrentToken.type == tokenType)
		{
			index++;
			return true;
		}
		return false;
	}

	private bool PeekMatch(TokenType tokenType)
	{
		if (HasMore)
		{
			return CurrentToken.type == tokenType;
		}
		return false;
	}

	private bool MatchId(out SyntaxTree factor)
	{
		Token currentToken = CurrentToken;
		if (currentToken.type == TokenType.TK_INPUT && Lexer.IsId(TokenValues[currentToken.sourceIdx]))
		{
			factor = new SyntaxTree(SyntaxTreeCode.FACTOR_ID, TokenValues[currentToken.sourceIdx]);
			index++;
			return true;
		}
		factor = null;
		return false;
	}

	private SyntaxTree NextParameterList()
	{
		if (Match(TokenType.TK_R_PAREN))
		{
			return new SyntaxTree(SyntaxTreeCode.FACTOR_PARAMS, string.Empty);
		}
		__command_latch = true;
		List<SyntaxTree> list = new List<SyntaxTree>();
		while (HasMore)
		{
			list.Add(NextExpr());
			if (Match(TokenType.TK_R_PAREN))
			{
				break;
			}
			Match(TokenType.TK_COMMA);
		}
		__command_latch = false;
		return new SyntaxTree(SyntaxTreeCode.FACTOR_PARAMS, string.Empty, list.ToArray());
	}

	private SyntaxTree NextParameterList_Command()
	{
		__command_latch = true;
		List<SyntaxTree> list = new List<SyntaxTree>();
		while (HasMore && !Match(TokenType.TK_EOF))
		{
			list.Add(NextExpr());
		}
		__command_latch = false;
		return new SyntaxTree(SyntaxTreeCode.FACTOR_PARAMS, string.Empty, list.ToArray());
	}

	private SyntaxTree NextFactor()
	{
		Token nextToken = NextToken;
		switch (nextToken.type)
		{
		case TokenType.TK_INT:
			return new SyntaxTree(SyntaxTreeCode.FACTOR_INT, TokenValues[nextToken.sourceIdx]);
		case TokenType.TK_FLOAT:
			return new SyntaxTree(SyntaxTreeCode.FACTOR_FLOAT, TokenValues[nextToken.sourceIdx]);
		case TokenType.TK_STRING:
			return new SyntaxTree(SyntaxTreeCode.FACTOR_STRING, TokenValues[nextToken.sourceIdx]);
		case TokenType.TK_NULL:
			return new SyntaxTree(SyntaxTreeCode.FACTOR_NULL, "null");
		case TokenType.TK_TRUE:
			return new SyntaxTree(SyntaxTreeCode.FACTOR_TRUE, "true");
		case TokenType.TK_FALSE:
			return new SyntaxTree(SyntaxTreeCode.FACTOR_FALSE, "false");
		case TokenType.TK_VAR:
			return new SyntaxTree(SyntaxTreeCode.OP_LOADVAR, TokenValues[nextToken.sourceIdx]);
		case TokenType.TK_INPUT:
		{
			if (Match(TokenType.TK_L_PAREN))
			{
				SyntaxTree l = new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[nextToken.sourceIdx]);
				return new SyntaxTree(SyntaxTreeCode.OP_CALL, string.Empty, l, NextParameterList());
			}
			if (__command_latch)
			{
				return new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[nextToken.sourceIdx]);
			}
			SyntaxTree l2 = new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[nextToken.sourceIdx]);
			return new SyntaxTree(SyntaxTreeCode.OP_CALL, string.Empty, l2, NextParameterList_Command());
		}
		case TokenType.TK_L_PAREN:
		{
			SyntaxTree result = NextExpr();
			if (!Match(TokenType.TK_R_PAREN))
			{
				throw new CommandSyntaxException("Missing ')' near \".." + lexerResult.NearInfo(nextToken) + "\"");
			}
			return result;
		}
		default:
			throw new CommandSyntaxException($"Unexpected token {nextToken.type} near \"..{lexerResult.NearInfo(nextToken)}\"");
		}
	}

	private SyntaxTree NextConverter()
	{
		SyntaxTree syntaxTree = NextFactor();
		if (Match(TokenType.TK_COLON))
		{
			Token nextToken = NextToken;
			if (nextToken.type == TokenType.TK_STRING || nextToken.type == TokenType.TK_INPUT)
			{
				SyntaxTree r = new SyntaxTree(SyntaxTreeCode.FACTOR_INPUT, TokenValues[nextToken.sourceIdx]);
				return new SyntaxTree(SyntaxTreeCode.OP_CVT, string.Empty, syntaxTree, r);
			}
			throw new CommandSyntaxException("Unexpected token near \".." + lexerResult.NearInfo(nextToken) + "\"");
		}
		return syntaxTree;
	}

	private SyntaxTree NextAccess()
	{
		SyntaxTree syntaxTree = NextConverter();
		while (HasMore)
		{
			int startIdx = CurrentToken.startIdx;
			if (Match(TokenType.TK_DOT))
			{
				if (MatchId(out var factor))
				{
					if (Match(TokenType.TK_L_PAREN))
					{
						SyntaxTree l = new SyntaxTree(SyntaxTreeCode.OP_DOT, string.Empty, syntaxTree, factor);
						syntaxTree = new SyntaxTree(SyntaxTreeCode.OP_CALL, string.Empty, l, NextParameterList());
					}
					else
					{
						syntaxTree = new SyntaxTree(SyntaxTreeCode.OP_DOT, string.Empty, syntaxTree, factor);
					}
					continue;
				}
				throw new CommandSyntaxException("Unexpected token near \".." + lexerResult.NearInfo(startIdx, CurrentToken.endIdx) + "\"");
			}
			if (!Match(TokenType.TK_L_BRACKET))
			{
				break;
			}
			SyntaxTree r = NextExpr();
			if (!Match(TokenType.TK_R_BRACKET))
			{
				throw new CommandSyntaxException("Missing ']' near \".." + lexerResult.NearInfo(startIdx, CurrentToken.endIdx) + "\"");
			}
			syntaxTree = new SyntaxTree(SyntaxTreeCode.OP_INDEX, string.Empty, syntaxTree, r);
		}
		return syntaxTree;
	}

	public SyntaxTree NextExpr(bool isRoot = false)
	{
		SyntaxTree syntaxTree = NextAccess();
		if (!HasMore || PeekMatch(TokenType.TK_EOF))
		{
			return syntaxTree;
		}
		if (Match(TokenType.TK_ASSIGN))
		{
			if (syntaxTree.opcode == SyntaxTreeCode.OP_DOT)
			{
				SyntaxTree syntaxTree2 = NextExpr();
				return new SyntaxTree(SyntaxTreeCode.OP_SET_FIELD, string.Empty, new SyntaxTree[3]
				{
					syntaxTree.children[0],
					syntaxTree.children[1],
					syntaxTree2
				});
			}
			if (syntaxTree.opcode == SyntaxTreeCode.OP_INDEX)
			{
				SyntaxTree syntaxTree3 = NextExpr();
				return new SyntaxTree(SyntaxTreeCode.OP_SET_ELEMENT, string.Empty, new SyntaxTree[3]
				{
					syntaxTree.children[0],
					syntaxTree.children[1],
					syntaxTree3
				});
			}
			return new SyntaxTree(SyntaxTreeCode.OP_ASSIGN, string.Empty, syntaxTree, NextExpr());
		}
		if (isRoot)
		{
			throw new CommandSyntaxException($"Unexpected token {CurrentToken.type} near \"..{lexerResult.NearInfo(CurrentToken)}\"");
		}
		return syntaxTree;
	}
}
