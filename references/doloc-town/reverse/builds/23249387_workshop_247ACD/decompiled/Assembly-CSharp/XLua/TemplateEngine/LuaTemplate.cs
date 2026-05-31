using System;
using System.Collections.Generic;
using System.Text;
using XLua.LuaDLL;

namespace XLua.TemplateEngine;

public class LuaTemplate
{
	private static lua_CSFunction templateCompileFunction = Compile;

	private static lua_CSFunction templateExecuteFunction = Execute;

	public static string ComposeCode(List<Chunk> chunks)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("local __text_gen = {}\r\n");
		foreach (Chunk chunk in chunks)
		{
			switch (chunk.Type)
			{
			case TokenType.Text:
				stringBuilder.Append("table.insert(__text_gen, \"" + chunk.Text + "\")\r\n");
				break;
			case TokenType.Eval:
				stringBuilder.Append("table.insert(__text_gen, tostring(" + chunk.Text + "))\r\n");
				break;
			case TokenType.Code:
				stringBuilder.Append(chunk.Text + "\r\n");
				break;
			}
		}
		stringBuilder.Append("return table.concat(__text_gen)\r\n");
		return stringBuilder.ToString();
	}

	public static LuaFunction Compile(LuaEnv luaenv, string snippet)
	{
		return luaenv.LoadString(ComposeCode(Parser.Parse(snippet)), "luatemplate");
	}

	public static string Execute(LuaFunction compiledTemplate, LuaTable parameters)
	{
		compiledTemplate.SetEnv(parameters);
		return compiledTemplate.Call()[0].ToString();
	}

	public static string Execute(LuaFunction compiledTemplate)
	{
		return compiledTemplate.Call()[0].ToString();
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int Compile(IntPtr L)
	{
		string snippet = Lua.lua_tostring(L, 1);
		string buff;
		try
		{
			buff = ComposeCode(Parser.Parse(snippet));
		}
		catch (Exception ex)
		{
			return Lua.luaL_error(L, $"template compile error:{ex.Message}\r\n");
		}
		if (Lua.luaL_loadbuffer(L, buff, "luatemplate") != 0)
		{
			return Lua.lua_error(L);
		}
		return 1;
	}

	[MonoPInvokeCallback(typeof(lua_CSFunction))]
	public static int Execute(IntPtr L)
	{
		if (!Lua.lua_isfunction(L, 1))
		{
			return Lua.luaL_error(L, "invalid compiled template, function needed!\r\n");
		}
		if (Lua.lua_istable(L, 2))
		{
			Lua.lua_setfenv(L, 1);
		}
		Lua.lua_pcall(L, 0, 1, 0);
		return 1;
	}

	public static void OpenLib(IntPtr L)
	{
		Lua.lua_newtable(L);
		Lua.xlua_pushasciistring(L, "compile");
		Lua.lua_pushstdcallcfunction(L, templateCompileFunction);
		Lua.lua_rawset(L, -3);
		Lua.xlua_pushasciistring(L, "execute");
		Lua.lua_pushstdcallcfunction(L, templateExecuteFunction);
		Lua.lua_rawset(L, -3);
		if (Lua.xlua_setglobal(L, "template") != 0)
		{
			throw new Exception("call xlua_setglobal fail!");
		}
	}
}
