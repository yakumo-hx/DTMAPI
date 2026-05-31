using System;
using System.Runtime.InteropServices;

namespace XLua.LuaDLL;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int lua_CSFunction(IntPtr L);
