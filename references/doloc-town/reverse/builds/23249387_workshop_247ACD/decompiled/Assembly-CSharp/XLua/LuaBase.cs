using System;
using XLua.LuaDLL;

namespace XLua;

public abstract class LuaBase : IDisposable
{
	protected bool disposed;

	protected readonly int luaReference;

	protected readonly LuaEnv luaEnv;

	public LuaBase(int reference, LuaEnv luaenv)
	{
		luaReference = reference;
		luaEnv = luaenv;
	}

	~LuaBase()
	{
		Dispose(disposeManagedResources: false);
	}

	public void Dispose()
	{
		Dispose(disposeManagedResources: true);
		GC.SuppressFinalize(this);
	}

	public virtual void Dispose(bool disposeManagedResources)
	{
		if (disposed)
		{
			return;
		}
		if (luaReference != 0)
		{
			bool flag = this is DelegateBridgeBase;
			if (disposeManagedResources)
			{
				luaEnv.translator.ReleaseLuaBase(luaEnv.L, luaReference, flag);
			}
			else
			{
				luaEnv.equeueGCAction(new LuaEnv.GCAction
				{
					Reference = luaReference,
					IsDelegate = flag
				});
			}
		}
		disposed = true;
	}

	public override bool Equals(object o)
	{
		if (o != null && GetType() == o.GetType())
		{
			LuaBase luaBase = (LuaBase)o;
			IntPtr l = luaEnv.L;
			if (l != luaBase.luaEnv.L)
			{
				return false;
			}
			int newTop = Lua.lua_gettop(l);
			Lua.lua_getref(l, luaBase.luaReference);
			Lua.lua_getref(l, luaReference);
			int num = Lua.lua_rawequal(l, -1, -2);
			Lua.lua_settop(l, newTop);
			return num != 0;
		}
		return false;
	}

	public override int GetHashCode()
	{
		Lua.lua_getref(luaEnv.L, luaReference);
		IntPtr intPtr = Lua.lua_topointer(luaEnv.L, -1);
		Lua.lua_pop(luaEnv.L, 1);
		return intPtr.ToInt32();
	}

	internal virtual void push(IntPtr L)
	{
		Lua.lua_getref(L, luaReference);
	}
}
