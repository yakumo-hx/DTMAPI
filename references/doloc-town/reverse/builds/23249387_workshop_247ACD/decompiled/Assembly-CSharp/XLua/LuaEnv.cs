using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XLua.LuaDLL;
using XLua.TemplateEngine;

namespace XLua;

public class LuaEnv : IDisposable
{
	internal struct GCAction
	{
		public int Reference;

		public bool IsDelegate;
	}

	public delegate byte[] CustomLoader(ref string filepath);

	public const string CSHARP_NAMESPACE = "xlua_csharp_namespace";

	public const string MAIN_SHREAD = "xlua_main_thread";

	internal IntPtr rawL;

	private LuaTable _G;

	internal ObjectTranslator translator;

	internal int errorFuncRef = -1;

	private const int LIB_VERSION_EXPECT = 105;

	private static List<Action<LuaEnv, ObjectTranslator>> initers;

	private int last_check_point;

	private int max_check_per_tick = 20;

	private Func<object, bool> object_valid_checker = ObjectValidCheck;

	private bool disposed;

	private Queue<GCAction> refQueue = new Queue<GCAction>();

	private string init_xlua = " \r\n            local metatable = {}\r\n            local rawget = rawget\r\n            local setmetatable = setmetatable\r\n            local import_type = xlua.import_type\r\n            local import_generic_type = xlua.import_generic_type\r\n            local load_assembly = xlua.load_assembly\r\n\r\n            function metatable:__index(key) \r\n                local fqn = rawget(self,'.fqn')\r\n                fqn = ((fqn and fqn .. '.') or '') .. key\r\n\r\n                local obj = import_type(fqn)\r\n\r\n                if obj == nil then\r\n                    -- It might be an assembly, so we load it too.\r\n                    obj = { ['.fqn'] = fqn }\r\n                    setmetatable(obj, metatable)\r\n                elseif obj == true then\r\n                    return rawget(self, key)\r\n                end\r\n\r\n                -- Cache this lookup\r\n                rawset(self, key, obj)\r\n                return obj\r\n            end\r\n\r\n            function metatable:__newindex()\r\n                error('No such type: ' .. rawget(self,'.fqn'), 2)\r\n            end\r\n\r\n            -- A non-type has been called; e.g. foo = System.Foo()\r\n            function metatable:__call(...)\r\n                local n = select('#', ...)\r\n                local fqn = rawget(self,'.fqn')\r\n                if n > 0 then\r\n                    local gt = import_generic_type(fqn, ...)\r\n                    if gt then\r\n                        return rawget(CS, gt)\r\n                    end\r\n                end\r\n                error('No such type: ' .. fqn, 2)\r\n            end\r\n\r\n            CS = CS or {}\r\n            setmetatable(CS, metatable)\r\n\r\n            typeof = function(t) return t.UnderlyingSystemType end\r\n            cast = xlua.cast\r\n            if not setfenv or not getfenv then\r\n                local function getfunction(level)\r\n                    local info = debug.getinfo(level + 1, 'f')\r\n                    return info and info.func\r\n                end\r\n\r\n                function setfenv(fn, env)\r\n                  if type(fn) == 'number' then fn = getfunction(fn + 1) end\r\n                  local i = 1\r\n                  while true do\r\n                    local name = debug.getupvalue(fn, i)\r\n                    if name == '_ENV' then\r\n                      debug.upvaluejoin(fn, i, (function()\r\n                        return env\r\n                      end), 1)\r\n                      break\r\n                    elseif not name then\r\n                      break\r\n                    end\r\n\r\n                    i = i + 1\r\n                  end\r\n\r\n                  return fn\r\n                end\r\n\r\n                function getfenv(fn)\r\n                  if type(fn) == 'number' then fn = getfunction(fn + 1) end\r\n                  local i = 1\r\n                  while true do\r\n                    local name, val = debug.getupvalue(fn, i)\r\n                    if name == '_ENV' then\r\n                      return val\r\n                    elseif not name then\r\n                      break\r\n                    end\r\n                    i = i + 1\r\n                  end\r\n                end\r\n            end\r\n\r\n            xlua.hotfix = function(cs, field, func)\r\n                if func == nil then func = false end\r\n                local tbl = (type(field) == 'table') and field or {[field] = func}\r\n                for k, v in pairs(tbl) do\r\n                    local cflag = ''\r\n                    if k == '.ctor' then\r\n                        cflag = '_c'\r\n                        k = 'ctor'\r\n                    end\r\n                    local f = type(v) == 'function' and v or nil\r\n                    xlua.access(cs, cflag .. '__Hotfix0_'..k, f) -- at least one\r\n                    pcall(function()\r\n                        for i = 1, 99 do\r\n                            xlua.access(cs, cflag .. '__Hotfix'..i..'_'..k, f)\r\n                        end\r\n                    end)\r\n                end\r\n                xlua.private_accessible(cs)\r\n            end\r\n            xlua.getmetatable = function(cs)\r\n                return xlua.metatable_operation(cs)\r\n            end\r\n            xlua.setmetatable = function(cs, mt)\r\n                return xlua.metatable_operation(cs, mt)\r\n            end\r\n            xlua.setclass = function(parent, name, impl)\r\n                impl.UnderlyingSystemType = parent[name].UnderlyingSystemType\r\n                rawset(parent, name, impl)\r\n            end\r\n            \r\n            local base_mt = {\r\n                __index = function(t, k)\r\n                    local csobj = t['__csobj']\r\n                    local func = csobj['<>xLuaBaseProxy_'..k]\r\n                    return function(_, ...)\r\n                         return func(csobj, ...)\r\n                    end\r\n                end\r\n            }\r\n            base = function(csobj)\r\n                return setmetatable({__csobj = csobj}, base_mt)\r\n            end\r\n            ";

	internal List<CustomLoader> customLoaders = new List<CustomLoader>();

	internal Dictionary<string, lua_CSFunction> buildin_initer = new Dictionary<string, lua_CSFunction>();

	internal IntPtr L
	{
		get
		{
			if (rawL == IntPtr.Zero)
			{
				throw new InvalidOperationException("this lua env had disposed!");
			}
			return rawL;
		}
	}

	public LuaTable Global => _G;

	public int GcPause
	{
		get
		{
			int num = Lua.lua_gc(L, LuaGCOptions.LUA_GCSETPAUSE, 200);
			Lua.lua_gc(L, LuaGCOptions.LUA_GCSETPAUSE, num);
			return num;
		}
		set
		{
			Lua.lua_gc(L, LuaGCOptions.LUA_GCSETPAUSE, value);
		}
	}

	public int GcStepmul
	{
		get
		{
			int num = Lua.lua_gc(L, LuaGCOptions.LUA_GCSETSTEPMUL, 200);
			Lua.lua_gc(L, LuaGCOptions.LUA_GCSETSTEPMUL, num);
			return num;
		}
		set
		{
			Lua.lua_gc(L, LuaGCOptions.LUA_GCSETSTEPMUL, value);
		}
	}

	public int Memroy => Lua.lua_gc(L, LuaGCOptions.LUA_GCCOUNT, 0);

	public LuaEnv()
	{
		if (Lua.xlua_get_lib_version() != 105)
		{
			throw new InvalidProgramException("wrong lib version expect:" + 105 + " but got:" + Lua.xlua_get_lib_version());
		}
		LuaIndexes.LUA_REGISTRYINDEX = Lua.xlua_get_registry_index();
		rawL = Lua.luaL_newstate();
		Lua.luaopen_xlua(rawL);
		Lua.luaopen_i64lib(rawL);
		translator = new ObjectTranslator(this, rawL);
		translator.createFunctionMetatable(rawL);
		translator.OpenLib(rawL);
		ObjectTranslatorPool.Instance.Add(rawL, translator);
		Lua.lua_atpanic(rawL, StaticLuaCallbacks.Panic);
		Lua.lua_pushstdcallcfunction(rawL, StaticLuaCallbacks.Print);
		if (Lua.xlua_setglobal(rawL, "print") != 0)
		{
			throw new Exception("call xlua_setglobal fail!");
		}
		LuaTemplate.OpenLib(rawL);
		AddSearcher(StaticLuaCallbacks.LoadBuiltinLib, 2);
		AddSearcher(StaticLuaCallbacks.LoadFromCustomLoaders, 3);
		AddSearcher(StaticLuaCallbacks.LoadFromResource, 4);
		AddSearcher(StaticLuaCallbacks.LoadFromStreamingAssetsPath, -1);
		DoString(init_xlua, "Init");
		init_xlua = null;
		AddBuildin("socket.core", StaticLuaCallbacks.LoadSocketCore);
		AddBuildin("socket", StaticLuaCallbacks.LoadSocketCore);
		AddBuildin("CS", StaticLuaCallbacks.LoadCS);
		Lua.lua_newtable(rawL);
		Lua.xlua_pushasciistring(rawL, "__index");
		Lua.lua_pushstdcallcfunction(rawL, StaticLuaCallbacks.MetaFuncIndex);
		Lua.lua_rawset(rawL, -3);
		Lua.xlua_pushasciistring(rawL, "LuaIndexs");
		Lua.lua_newtable(rawL);
		Lua.lua_pushvalue(rawL, -3);
		Lua.lua_setmetatable(rawL, -2);
		Lua.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.xlua_pushasciistring(rawL, "LuaNewIndexs");
		Lua.lua_newtable(rawL);
		Lua.lua_pushvalue(rawL, -3);
		Lua.lua_setmetatable(rawL, -2);
		Lua.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.xlua_pushasciistring(rawL, "LuaClassIndexs");
		Lua.lua_newtable(rawL);
		Lua.lua_pushvalue(rawL, -3);
		Lua.lua_setmetatable(rawL, -2);
		Lua.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.xlua_pushasciistring(rawL, "LuaClassNewIndexs");
		Lua.lua_newtable(rawL);
		Lua.lua_pushvalue(rawL, -3);
		Lua.lua_setmetatable(rawL, -2);
		Lua.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.lua_pop(rawL, 1);
		Lua.xlua_pushasciistring(rawL, "xlua_main_thread");
		Lua.lua_pushthread(rawL);
		Lua.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
		Lua.xlua_pushasciistring(rawL, "xlua_csharp_namespace");
		if (Lua.xlua_getglobal(rawL, "CS") != 0)
		{
			throw new Exception("get CS fail!");
		}
		Lua.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
		translator.Alias(typeof(Type), "System.MonoType");
		if (Lua.xlua_getglobal(rawL, "_G") != 0)
		{
			throw new Exception("get _G fail!");
		}
		translator.Get(rawL, -1, out _G);
		Lua.lua_pop(rawL, 1);
		errorFuncRef = Lua.get_error_func_ref(rawL);
		if (initers != null)
		{
			for (int i = 0; i < initers.Count; i++)
			{
				initers[i](this, translator);
			}
		}
		translator.CreateArrayMetatable(rawL);
		translator.CreateDelegateMetatable(rawL);
		translator.CreateEnumerablePairs(rawL);
	}

	public static void AddIniter(Action<LuaEnv, ObjectTranslator> initer)
	{
		if (initers == null)
		{
			initers = new List<Action<LuaEnv, ObjectTranslator>>();
		}
		initers.Add(initer);
	}

	public T LoadString<T>(byte[] chunk, string chunkName = "chunk", LuaTable env = null)
	{
		if (typeof(T) != typeof(LuaFunction) && !typeof(T).IsSubclassOf(typeof(Delegate)))
		{
			throw new InvalidOperationException(typeof(T).Name + " is not a delegate type nor LuaFunction");
		}
		IntPtr l = L;
		int num = Lua.lua_gettop(l);
		if (Lua.xluaL_loadbuffer(l, chunk, chunk.Length, chunkName) != 0)
		{
			ThrowExceptionFromError(num);
		}
		if (env != null)
		{
			env.push(l);
			Lua.lua_setfenv(l, -2);
		}
		T result = (T)translator.GetObject(l, -1, typeof(T));
		Lua.lua_settop(l, num);
		return result;
	}

	public T LoadString<T>(string chunk, string chunkName = "chunk", LuaTable env = null)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(chunk);
		return LoadString<T>(bytes, chunkName, env);
	}

	public LuaFunction LoadString(string chunk, string chunkName = "chunk", LuaTable env = null)
	{
		return LoadString<LuaFunction>(chunk, chunkName, env);
	}

	public object[] DoString(byte[] chunk, string chunkName = "chunk", LuaTable env = null)
	{
		IntPtr l = L;
		int oldTop = Lua.lua_gettop(l);
		int num = Lua.load_error_func(l, errorFuncRef);
		if (Lua.xluaL_loadbuffer(l, chunk, chunk.Length, chunkName) == 0)
		{
			if (env != null)
			{
				env.push(l);
				Lua.lua_setfenv(l, -2);
			}
			if (Lua.lua_pcall(l, 0, -1, num) == 0)
			{
				Lua.lua_remove(l, num);
				return translator.popValues(l, oldTop);
			}
			ThrowExceptionFromError(oldTop);
		}
		else
		{
			ThrowExceptionFromError(oldTop);
		}
		return null;
	}

	public object[] DoString(string chunk, string chunkName = "chunk", LuaTable env = null)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(chunk);
		return DoString(bytes, chunkName, env);
	}

	private void AddSearcher(lua_CSFunction searcher, int index)
	{
		IntPtr l = L;
		Lua.xlua_getloaders(l);
		if (!Lua.lua_istable(l, -1))
		{
			throw new Exception("Can not set searcher!");
		}
		uint num = Lua.xlua_objlen(l, -1);
		index = (int)((index < 0) ? (num + index + 2) : index);
		for (int num2 = (int)(num + 1); num2 > index; num2--)
		{
			Lua.xlua_rawgeti(l, -1, num2 - 1);
			Lua.xlua_rawseti(l, -2, num2);
		}
		Lua.lua_pushstdcallcfunction(l, searcher);
		Lua.xlua_rawseti(l, -2, index);
		Lua.lua_pop(l, 1);
	}

	public void Alias(Type type, string alias)
	{
		translator.Alias(type, alias);
	}

	private static bool ObjectValidCheck(object obj)
	{
		if (obj is UnityEngine.Object)
		{
			return obj as UnityEngine.Object != null;
		}
		return true;
	}

	public void Tick()
	{
		IntPtr l = L;
		lock (refQueue)
		{
			while (refQueue.Count > 0)
			{
				GCAction gCAction = refQueue.Dequeue();
				translator.ReleaseLuaBase(l, gCAction.Reference, gCAction.IsDelegate);
			}
		}
		last_check_point = translator.objects.Check(last_check_point, max_check_per_tick, object_valid_checker, translator.reverseMap);
	}

	public void GC()
	{
		Tick();
	}

	public LuaTable NewTable()
	{
		IntPtr l = L;
		int newTop = Lua.lua_gettop(l);
		Lua.lua_newtable(l);
		LuaTable result = (LuaTable)translator.GetObject(l, -1, typeof(LuaTable));
		Lua.lua_settop(l, newTop);
		return result;
	}

	public void Dispose()
	{
		FullGc();
		System.GC.Collect();
		System.GC.WaitForPendingFinalizers();
		Dispose(dispose: true);
		System.GC.Collect();
		System.GC.WaitForPendingFinalizers();
	}

	public virtual void Dispose(bool dispose)
	{
		if (!disposed)
		{
			Tick();
			if (!translator.AllDelegateBridgeReleased())
			{
				throw new InvalidOperationException("try to dispose a LuaEnv with C# callback!");
			}
			ObjectTranslatorPool.Instance.Remove(L);
			Lua.lua_close(L);
			translator = null;
			rawL = IntPtr.Zero;
			disposed = true;
		}
	}

	public void ThrowExceptionFromError(int oldTop)
	{
		object obj = translator.GetObject(L, -1);
		Lua.lua_settop(L, oldTop);
		if (obj is Exception ex)
		{
			throw ex;
		}
		if (obj == null)
		{
			obj = "Unknown Lua Error";
		}
		throw new LuaException(obj.ToString());
	}

	internal void equeueGCAction(GCAction action)
	{
		lock (refQueue)
		{
			refQueue.Enqueue(action);
		}
	}

	public void AddLoader(CustomLoader loader)
	{
		customLoaders.Add(loader);
	}

	public void AddBuildin(string name, lua_CSFunction initer)
	{
		if (!Utils.IsStaticPInvokeCSFunction(initer))
		{
			throw new Exception("initer must be static and has MonoPInvokeCallback Attribute!");
		}
		buildin_initer.Add(name, initer);
	}

	public void FullGc()
	{
		Lua.lua_gc(L, LuaGCOptions.LUA_GCCOLLECT, 0);
	}

	public void StopGc()
	{
		Lua.lua_gc(L, LuaGCOptions.LUA_GCSTOP, 0);
	}

	public void RestartGc()
	{
		Lua.lua_gc(L, LuaGCOptions.LUA_GCRESTART, 0);
	}

	public bool GcStep(int data)
	{
		return Lua.lua_gc(L, LuaGCOptions.LUA_GCSTEP, data) != 0;
	}
}
