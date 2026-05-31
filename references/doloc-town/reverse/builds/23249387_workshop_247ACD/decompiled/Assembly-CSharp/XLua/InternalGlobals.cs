using System;
using System.Collections.Generic;
using System.Reflection;
using XLua.LuaDLL;

namespace XLua;

internal class InternalGlobals
{
	internal delegate bool TryArrayGet(Type type, IntPtr L, ObjectTranslator translator, object obj, int index);

	internal delegate bool TryArraySet(Type type, IntPtr L, ObjectTranslator translator, object obj, int array_idx, int obj_idx);

	internal static byte[] strBuff;

	internal static volatile TryArrayGet genTryArrayGetPtr;

	internal static volatile TryArraySet genTryArraySetPtr;

	internal static volatile ObjectTranslatorPool objectTranslatorPool;

	internal static volatile int LUA_REGISTRYINDEX;

	internal static volatile Dictionary<string, string> supportOp;

	internal static volatile Dictionary<Type, IEnumerable<MethodInfo>> extensionMethodMap;

	internal static volatile lua_CSFunction LazyReflectionWrap;

	static InternalGlobals()
	{
		strBuff = new byte[256];
		genTryArrayGetPtr = null;
		genTryArraySetPtr = null;
		objectTranslatorPool = new ObjectTranslatorPool();
		LUA_REGISTRYINDEX = -10000;
		supportOp = new Dictionary<string, string>
		{
			{ "op_Addition", "__add" },
			{ "op_Subtraction", "__sub" },
			{ "op_Multiply", "__mul" },
			{ "op_Division", "__div" },
			{ "op_Equality", "__eq" },
			{ "op_UnaryNegation", "__unm" },
			{ "op_LessThan", "__lt" },
			{ "op_LessThanOrEqual", "__le" },
			{ "op_Modulus", "__mod" },
			{ "op_BitwiseAnd", "__band" },
			{ "op_BitwiseOr", "__bor" },
			{ "op_ExclusiveOr", "__bxor" },
			{ "op_OnesComplement", "__bnot" },
			{ "op_LeftShift", "__shl" },
			{ "op_RightShift", "__shr" }
		};
		extensionMethodMap = null;
		LazyReflectionWrap = Utils.LazyReflectionCall;
		extensionMethodMap = new Dictionary<Type, IEnumerable<MethodInfo>>();
		genTryArrayGetPtr = StaticLuaCallbacks.__tryArrayGet;
		genTryArraySetPtr = StaticLuaCallbacks.__tryArraySet;
	}
}
