using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

internal static class DefaultTypeParserDefination
{
	public static Dictionary<Type, ValueParser> DefaultTypeParser => new Dictionary<Type, ValueParser>
	{
		{
			typeof(int),
			TryParseInt
		},
		{
			typeof(float),
			TryParseFloat
		},
		{
			typeof(double),
			TryParseDouble
		},
		{
			typeof(bool),
			TryParseBool
		},
		{
			typeof(string),
			TryParseString
		},
		{
			typeof(char),
			TryParseChar
		},
		{
			typeof(byte),
			TryParseByte
		},
		{
			typeof(sbyte),
			TryParseSByte
		},
		{
			typeof(short),
			TryParseShort
		},
		{
			typeof(ushort),
			TryParseUShort
		},
		{
			typeof(uint),
			TryParseUInt
		},
		{
			typeof(long),
			TryParseLong
		},
		{
			typeof(ulong),
			TryParseULong
		},
		{
			typeof(decimal),
			TryParseDecimal
		}
	};

	public static Dictionary<string, Type> DefaultTypeAlias => new Dictionary<string, Type>
	{
		{
			"int",
			typeof(int)
		},
		{
			"float",
			typeof(float)
		},
		{
			"double",
			typeof(double)
		},
		{
			"bool",
			typeof(bool)
		},
		{
			"string",
			typeof(string)
		},
		{
			"char",
			typeof(char)
		},
		{
			"byte",
			typeof(byte)
		},
		{
			"sbyte",
			typeof(sbyte)
		},
		{
			"short",
			typeof(short)
		},
		{
			"ushort",
			typeof(ushort)
		},
		{
			"uint",
			typeof(uint)
		},
		{
			"long",
			typeof(long)
		},
		{
			"ulong",
			typeof(ulong)
		},
		{
			"decimal",
			typeof(decimal)
		}
	};

	private static bool TryParseInt(string input, out object result)
	{
		if (int.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseFloat(string input, out object result)
	{
		if (float.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseDouble(string input, out object result)
	{
		if (double.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseBool(string input, out object result)
	{
		if (bool.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseString(string input, out object result)
	{
		result = input;
		return true;
	}

	private static bool TryParseChar(string input, out object result)
	{
		if (char.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseByte(string input, out object result)
	{
		if (byte.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseSByte(string input, out object result)
	{
		if (sbyte.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseShort(string input, out object result)
	{
		if (short.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseUShort(string input, out object result)
	{
		if (ushort.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseUInt(string input, out object result)
	{
		if (uint.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseLong(string input, out object result)
	{
		if (long.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseULong(string input, out object result)
	{
		if (ulong.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}

	private static bool TryParseDecimal(string input, out object result)
	{
		if (decimal.TryParse(input, out var result2))
		{
			result = result2;
			return true;
		}
		result = null;
		return false;
	}
}
