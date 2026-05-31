using System;
using System.Linq;

namespace RedSaw.CommandLineInterface;

public class SyntaxTree
{
	public readonly string data;

	public readonly SyntaxTreeCode opcode;

	public readonly SyntaxTree[] children;

	public SyntaxTree L => children[0];

	public SyntaxTree R => children[1];

	public bool IsLeaf => children.Length == 0;

	public string DebugInfo => opcode switch
	{
		SyntaxTreeCode.OP_DOT => L?.DebugInfo + "." + R?.DebugInfo, 
		SyntaxTreeCode.OP_CALL => L?.DebugInfo + R?.DebugInfo, 
		SyntaxTreeCode.OP_INDEX => L?.DebugInfo + "[" + R?.DebugInfo + "]", 
		SyntaxTreeCode.OP_ASSIGN => L?.DebugInfo + " = " + R?.DebugInfo, 
		SyntaxTreeCode.OP_SET_FIELD => L?.DebugInfo + "." + R?.DebugInfo + " = " + children[2]?.DebugInfo, 
		SyntaxTreeCode.OP_CVT => L?.DebugInfo + ":" + R?.DebugInfo, 
		SyntaxTreeCode.FACTOR_PARAMS => "(" + string.Join(", ", children.Select((SyntaxTree x) => x.DebugInfo)) + ")", 
		SyntaxTreeCode.FACTOR_FLOAT => data + "f", 
		SyntaxTreeCode.FACTOR_STRING => data, 
		SyntaxTreeCode.FACTOR_TRUE => data, 
		SyntaxTreeCode.FACTOR_FALSE => data, 
		SyntaxTreeCode.FACTOR_NULL => data, 
		SyntaxTreeCode.FACTOR_INT => data, 
		SyntaxTreeCode.FACTOR_ID => "@" + data, 
		SyntaxTreeCode.FACTOR_INPUT => "?" + data, 
		_ => ToString(), 
	};

	public SyntaxTree(SyntaxTreeCode opcode, string data)
	{
		this.opcode = opcode;
		this.data = data;
		children = Array.Empty<SyntaxTree>();
	}

	public SyntaxTree(SyntaxTreeCode opcode, string data, SyntaxTree L)
	{
		this.opcode = opcode;
		this.data = data;
		children = new SyntaxTree[1] { L };
	}

	public SyntaxTree(SyntaxTreeCode opcode, string data, SyntaxTree L, SyntaxTree R)
	{
		this.opcode = opcode;
		this.data = data;
		children = new SyntaxTree[2] { L, R };
	}

	public SyntaxTree(SyntaxTreeCode opcode, string data, SyntaxTree[] children)
	{
		this.opcode = opcode;
		this.data = data;
		this.children = children;
	}

	public override string ToString()
	{
		if (data != null || data.Length > 0)
		{
			return data;
		}
		return opcode.ToString();
	}
}
