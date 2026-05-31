using System;

namespace XLua;

public enum GenFlag
{
	No,
	[Obsolete("use GCOptimizeAttribute instead")]
	GCOptimize
}
