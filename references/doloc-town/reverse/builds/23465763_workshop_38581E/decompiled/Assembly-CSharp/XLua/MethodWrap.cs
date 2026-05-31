using System;
using System.Collections.Generic;
using System.Reflection;
using XLua.LuaDLL;

namespace XLua;

public class MethodWrap
{
	private string methodName;

	private List<OverloadMethodWrap> overloads = new List<OverloadMethodWrap>();

	private bool forceCheck;

	public MethodWrap(string methodName, List<OverloadMethodWrap> overloads, bool forceCheck)
	{
		this.methodName = methodName;
		this.overloads = overloads;
		this.forceCheck = forceCheck;
	}

	public int Call(IntPtr L)
	{
		try
		{
			if (overloads.Count == 1 && !overloads[0].HasDefalutValue && !forceCheck)
			{
				return overloads[0].Call(L);
			}
			for (int i = 0; i < overloads.Count; i++)
			{
				OverloadMethodWrap overloadMethodWrap = overloads[i];
				if (overloadMethodWrap.Check(L))
				{
					return overloadMethodWrap.Call(L);
				}
			}
			return Lua.luaL_error(L, "invalid arguments to " + methodName);
		}
		catch (TargetInvocationException ex)
		{
			return Lua.luaL_error(L, "c# exception:" + ex.InnerException.Message + ",stack:" + ex.InnerException.StackTrace);
		}
		catch (Exception ex2)
		{
			return Lua.luaL_error(L, "c# exception:" + ex2.Message + ",stack:" + ex2.StackTrace);
		}
	}
}
