using System;
using System.Collections.Generic;
using XLua.LuaDLL;

namespace XLua;

public class ObjectTranslatorPool
{
	private Dictionary<IntPtr, WeakReference> translators = new Dictionary<IntPtr, WeakReference>();

	private IntPtr lastPtr;

	private ObjectTranslator lastTranslator;

	public static ObjectTranslatorPool Instance => InternalGlobals.objectTranslatorPool;

	public void Add(IntPtr L, ObjectTranslator translator)
	{
		lastTranslator = translator;
		IntPtr key = (lastPtr = Lua.xlua_gl(L));
		translators.Add(key, new WeakReference(translator));
	}

	public ObjectTranslator Find(IntPtr L)
	{
		IntPtr intPtr = Lua.xlua_gl(L);
		if (lastPtr == intPtr)
		{
			return lastTranslator;
		}
		if (translators.ContainsKey(intPtr))
		{
			lastPtr = intPtr;
			lastTranslator = translators[intPtr].Target as ObjectTranslator;
			return lastTranslator;
		}
		return null;
	}

	public void Remove(IntPtr L)
	{
		IntPtr intPtr = Lua.xlua_gl(L);
		if (translators.ContainsKey(intPtr))
		{
			if (lastPtr == intPtr)
			{
				lastPtr = default(IntPtr);
				lastTranslator = null;
			}
			translators.Remove(intPtr);
		}
	}
}
