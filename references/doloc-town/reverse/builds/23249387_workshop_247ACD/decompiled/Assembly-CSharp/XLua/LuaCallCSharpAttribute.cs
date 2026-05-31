using System;

namespace XLua;

public class LuaCallCSharpAttribute : Attribute
{
	private GenFlag flag;

	public GenFlag Flag => flag;

	public LuaCallCSharpAttribute(GenFlag flag = GenFlag.No)
	{
		this.flag = flag;
	}
}
