using System;

namespace XLua;

public class HotfixAttribute : Attribute
{
	private HotfixFlag flag;

	public HotfixFlag Flag => flag;

	public HotfixAttribute(HotfixFlag e = HotfixFlag.Stateless)
	{
		flag = e;
	}
}
