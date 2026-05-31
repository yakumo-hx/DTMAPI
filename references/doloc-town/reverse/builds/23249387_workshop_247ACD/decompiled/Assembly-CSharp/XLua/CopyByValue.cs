using System;
using UnityEngine;
using XLua.LuaDLL;

namespace XLua;

public static class CopyByValue
{
	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Vector2 val)
	{
		val = default(Vector2);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "x"))
		{
			translator.Get(L, num + 1, out val.x);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "y"))
		{
			translator.Get(L, num + 1, out val.y);
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Vector2 field)
	{
		if (!Lua.xlua_pack_float2(buff, offset, field.x, field.y))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Vector2 field)
	{
		field = default(Vector2);
		float f = 0f;
		float f2 = 0f;
		if (!Lua.xlua_unpack_float2(buff, offset, out f, out f2))
		{
			return false;
		}
		field.x = f;
		field.y = f2;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Vector3 val)
	{
		val = default(Vector3);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "x"))
		{
			translator.Get(L, num + 1, out val.x);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "y"))
		{
			translator.Get(L, num + 1, out val.y);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "z"))
		{
			translator.Get(L, num + 1, out val.z);
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Vector3 field)
	{
		if (!Lua.xlua_pack_float3(buff, offset, field.x, field.y, field.z))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Vector3 field)
	{
		field = default(Vector3);
		float f = 0f;
		float f2 = 0f;
		float f3 = 0f;
		if (!Lua.xlua_unpack_float3(buff, offset, out f, out f2, out f3))
		{
			return false;
		}
		field.x = f;
		field.y = f2;
		field.z = f3;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Vector4 val)
	{
		val = default(Vector4);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "x"))
		{
			translator.Get(L, num + 1, out val.x);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "y"))
		{
			translator.Get(L, num + 1, out val.y);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "z"))
		{
			translator.Get(L, num + 1, out val.z);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "w"))
		{
			translator.Get(L, num + 1, out val.w);
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Vector4 field)
	{
		if (!Lua.xlua_pack_float4(buff, offset, field.x, field.y, field.z, field.w))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Vector4 field)
	{
		field = default(Vector4);
		float f = 0f;
		float f2 = 0f;
		float f3 = 0f;
		float f4 = 0f;
		if (!Lua.xlua_unpack_float4(buff, offset, out f, out f2, out f3, out f4))
		{
			return false;
		}
		field.x = f;
		field.y = f2;
		field.z = f3;
		field.w = f4;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Color val)
	{
		val = default(Color);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "r"))
		{
			translator.Get(L, num + 1, out val.r);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "g"))
		{
			translator.Get(L, num + 1, out val.g);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "b"))
		{
			translator.Get(L, num + 1, out val.b);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "a"))
		{
			translator.Get(L, num + 1, out val.a);
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Color field)
	{
		if (!Lua.xlua_pack_float4(buff, offset, field.r, field.g, field.b, field.a))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Color field)
	{
		field = default(Color);
		float f = 0f;
		float f2 = 0f;
		float f3 = 0f;
		float f4 = 0f;
		if (!Lua.xlua_unpack_float4(buff, offset, out f, out f2, out f3, out f4))
		{
			return false;
		}
		field.r = f;
		field.g = f2;
		field.b = f3;
		field.a = f4;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Quaternion val)
	{
		val = default(Quaternion);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "x"))
		{
			translator.Get(L, num + 1, out val.x);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "y"))
		{
			translator.Get(L, num + 1, out val.y);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "z"))
		{
			translator.Get(L, num + 1, out val.z);
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "w"))
		{
			translator.Get(L, num + 1, out val.w);
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Quaternion field)
	{
		if (!Lua.xlua_pack_float4(buff, offset, field.x, field.y, field.z, field.w))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Quaternion field)
	{
		field = default(Quaternion);
		float f = 0f;
		float f2 = 0f;
		float f3 = 0f;
		float f4 = 0f;
		if (!Lua.xlua_unpack_float4(buff, offset, out f, out f2, out f3, out f4))
		{
			return false;
		}
		field.x = f;
		field.y = f2;
		field.z = f3;
		field.w = f4;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Ray val)
	{
		val = default(Ray);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "origin"))
		{
			Vector3 val2 = val.origin;
			translator.Get(L, num + 1, out val2);
			val.origin = val2;
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "direction"))
		{
			Vector3 val3 = val.direction;
			translator.Get(L, num + 1, out val3);
			val.direction = val3;
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Ray field)
	{
		if (!Pack(buff, offset, field.origin))
		{
			return false;
		}
		if (!Pack(buff, offset + 12, field.direction))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Ray field)
	{
		field = default(Ray);
		Vector3 field2 = field.origin;
		if (!UnPack(buff, offset, out field2))
		{
			return false;
		}
		field.origin = field2;
		Vector3 field3 = field.direction;
		if (!UnPack(buff, offset + 12, out field3))
		{
			return false;
		}
		field.direction = field3;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Bounds val)
	{
		val = default(Bounds);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "center"))
		{
			Vector3 val2 = val.center;
			translator.Get(L, num + 1, out val2);
			val.center = val2;
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "extents"))
		{
			Vector3 val3 = val.extents;
			translator.Get(L, num + 1, out val3);
			val.extents = val3;
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Bounds field)
	{
		if (!Pack(buff, offset, field.center))
		{
			return false;
		}
		if (!Pack(buff, offset + 12, field.extents))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Bounds field)
	{
		field = default(Bounds);
		Vector3 field2 = field.center;
		if (!UnPack(buff, offset, out field2))
		{
			return false;
		}
		field.center = field2;
		Vector3 field3 = field.extents;
		if (!UnPack(buff, offset + 12, out field3))
		{
			return false;
		}
		field.extents = field3;
		return true;
	}

	public static void UnPack(ObjectTranslator translator, IntPtr L, int idx, out Ray2D val)
	{
		val = default(Ray2D);
		int num = Lua.lua_gettop(L);
		if (Utils.LoadField(L, idx, "origin"))
		{
			Vector2 val2 = val.origin;
			translator.Get(L, num + 1, out val2);
			val.origin = val2;
		}
		Lua.lua_pop(L, 1);
		if (Utils.LoadField(L, idx, "direction"))
		{
			Vector2 val3 = val.direction;
			translator.Get(L, num + 1, out val3);
			val.direction = val3;
		}
		Lua.lua_pop(L, 1);
	}

	public static bool Pack(IntPtr buff, int offset, Ray2D field)
	{
		if (!Pack(buff, offset, field.origin))
		{
			return false;
		}
		if (!Pack(buff, offset + 8, field.direction))
		{
			return false;
		}
		return true;
	}

	public static bool UnPack(IntPtr buff, int offset, out Ray2D field)
	{
		field = default(Ray2D);
		Vector2 field2 = field.origin;
		if (!UnPack(buff, offset, out field2))
		{
			return false;
		}
		field.origin = field2;
		Vector2 field3 = field.direction;
		if (!UnPack(buff, offset + 8, out field3))
		{
			return false;
		}
		field.direction = field3;
		return true;
	}

	public static bool Pack(IntPtr buff, int offset, byte field)
	{
		return Lua.xlua_pack_int8_t(buff, offset, field);
	}

	public static bool UnPack(IntPtr buff, int offset, out byte field)
	{
		return Lua.xlua_unpack_int8_t(buff, offset, out field);
	}

	public static bool Pack(IntPtr buff, int offset, sbyte field)
	{
		return Lua.xlua_pack_int8_t(buff, offset, (byte)field);
	}

	public static bool UnPack(IntPtr buff, int offset, out sbyte field)
	{
		byte field2;
		bool result = Lua.xlua_unpack_int8_t(buff, offset, out field2);
		field = (sbyte)field2;
		return result;
	}

	public static bool Pack(IntPtr buff, int offset, short field)
	{
		return Lua.xlua_pack_int16_t(buff, offset, field);
	}

	public static bool UnPack(IntPtr buff, int offset, out short field)
	{
		return Lua.xlua_unpack_int16_t(buff, offset, out field);
	}

	public static bool Pack(IntPtr buff, int offset, ushort field)
	{
		return Lua.xlua_pack_int16_t(buff, offset, (short)field);
	}

	public static bool UnPack(IntPtr buff, int offset, out ushort field)
	{
		short field2;
		bool result = Lua.xlua_unpack_int16_t(buff, offset, out field2);
		field = (ushort)field2;
		return result;
	}

	public static bool Pack(IntPtr buff, int offset, int field)
	{
		return Lua.xlua_pack_int32_t(buff, offset, field);
	}

	public static bool UnPack(IntPtr buff, int offset, out int field)
	{
		return Lua.xlua_unpack_int32_t(buff, offset, out field);
	}

	public static bool Pack(IntPtr buff, int offset, uint field)
	{
		return Lua.xlua_pack_int32_t(buff, offset, (int)field);
	}

	public static bool UnPack(IntPtr buff, int offset, out uint field)
	{
		int field2;
		bool result = Lua.xlua_unpack_int32_t(buff, offset, out field2);
		field = (uint)field2;
		return result;
	}

	public static bool Pack(IntPtr buff, int offset, long field)
	{
		return Lua.xlua_pack_int64_t(buff, offset, field);
	}

	public static bool UnPack(IntPtr buff, int offset, out long field)
	{
		return Lua.xlua_unpack_int64_t(buff, offset, out field);
	}

	public static bool Pack(IntPtr buff, int offset, ulong field)
	{
		return Lua.xlua_pack_int64_t(buff, offset, (long)field);
	}

	public static bool UnPack(IntPtr buff, int offset, out ulong field)
	{
		long field2;
		bool result = Lua.xlua_unpack_int64_t(buff, offset, out field2);
		field = (ulong)field2;
		return result;
	}

	public static bool Pack(IntPtr buff, int offset, float field)
	{
		return Lua.xlua_pack_float(buff, offset, field);
	}

	public static bool UnPack(IntPtr buff, int offset, out float field)
	{
		return Lua.xlua_unpack_float(buff, offset, out field);
	}

	public static bool Pack(IntPtr buff, int offset, double field)
	{
		return Lua.xlua_pack_double(buff, offset, field);
	}

	public static bool UnPack(IntPtr buff, int offset, out double field)
	{
		return Lua.xlua_unpack_double(buff, offset, out field);
	}

	public static bool Pack(IntPtr buff, int offset, decimal field)
	{
		return Lua.xlua_pack_decimal(buff, offset, ref field);
	}

	public static bool UnPack(IntPtr buff, int offset, out decimal field)
	{
		if (!Lua.xlua_unpack_decimal(buff, offset, out var scale, out var sign, out var hi, out var lo))
		{
			field = default(decimal);
			return false;
		}
		field = new decimal((int)(lo & 0xFFFFFFFFu), (int)(lo >> 32), hi, (sign & 0x80) != 0, scale);
		return true;
	}

	public static bool IsStruct(Type type)
	{
		if (type.IsValueType() && !type.IsEnum())
		{
			return !type.IsPrimitive();
		}
		return false;
	}
}
