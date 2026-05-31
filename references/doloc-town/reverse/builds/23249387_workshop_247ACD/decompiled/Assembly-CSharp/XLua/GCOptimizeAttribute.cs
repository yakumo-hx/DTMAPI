using System;

namespace XLua;

public class GCOptimizeAttribute : Attribute
{
	private OptimizeFlag flag;

	public OptimizeFlag Flag => flag;

	public GCOptimizeAttribute(OptimizeFlag flag = OptimizeFlag.Default)
	{
		this.flag = flag;
	}
}
