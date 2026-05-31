using System;
using System.Collections.Generic;

namespace XLua;

public abstract class DelegateBridgeBase : LuaBase
{
	private Type firstKey;

	private Delegate firstValue;

	private Dictionary<Type, Delegate> bindTo;

	protected int errorFuncRef;

	public DelegateBridgeBase(int reference, LuaEnv luaenv)
		: base(reference, luaenv)
	{
		errorFuncRef = luaenv.errorFuncRef;
	}

	public bool TryGetDelegate(Type key, out Delegate value)
	{
		if (key == firstKey)
		{
			value = firstValue;
			return true;
		}
		if (bindTo != null)
		{
			return bindTo.TryGetValue(key, out value);
		}
		value = null;
		return false;
	}

	public void AddDelegate(Type key, Delegate value)
	{
		if (key == firstKey)
		{
			throw new ArgumentException("An element with the same key already exists in the dictionary.");
		}
		if (firstKey == null && bindTo == null)
		{
			firstKey = key;
			firstValue = value;
		}
		else if (firstKey != null && bindTo == null)
		{
			bindTo = new Dictionary<Type, Delegate>();
			bindTo.Add(firstKey, firstValue);
			firstKey = null;
			firstValue = null;
			bindTo.Add(key, value);
		}
		else
		{
			bindTo.Add(key, value);
		}
	}

	public virtual Delegate GetDelegateByType(Type type)
	{
		return null;
	}
}
